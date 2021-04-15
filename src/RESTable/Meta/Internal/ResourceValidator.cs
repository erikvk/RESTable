using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
using RESTable.Admin;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Linq;
using RESTable.Meta.IL;

namespace RESTable.Meta.Internal
{
    public class ResourceValidator
    {
        private TypeCache TypeCache { get; }
        private ResourceCollection ResourceCollection { get; }
        private RESTableConfiguration Configuration { get; }

        public ResourceValidator(TypeCache typeCache, ResourceCollection resourceCollection, RESTableConfiguration configuration)
        {
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
            Configuration = configuration;
        }

        public void ValidateRuntimeInsertion(Type type, string fullName, RESTableAttribute attribute)
        {
            string name;
            if (fullName != null)
            {
                // if (fullName.StartsWith("RESTable.", StringComparison.OrdinalIgnoreCase) && !type.Assembly.Equals(typeof(ResourceValidator).Assembly))
                //     throw new InvalidResourceDeclarationException($"Cannot add resource '{fullName}'. A resource name cannot start with 'RESTable'");
                name = fullName;
            }
            else name = type.GetRESTableTypeName();
            if (name == null)
                throw new InvalidResourceDeclarationException(
                    "Encountered an unknown type. No further information is available.");
            if (ResourceCollection.ResourceByType.ContainsKey(type))
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. A resource with the same type ('{type.GetRESTableTypeName()}') has already been added to RESTable");
            if (ResourceCollection.ResourceByName.ContainsKey(name))
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. A resource with the same name has already been added to RESTable");
            attribute ??= type.GetCustomAttribute<RESTableAttribute>();
            if (attribute == null)
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. The type was not decorated with the RESTableAttribute attribute, and " +
                    "no additional attribute instance was included in the insertion.");
            Validate(type);
        }

        public (List<Type> regular, List<Type> wrappers, List<Type> terminals, List<Type> binaries, List<Type> events) Validate(params Type[] types)
        {
            var entityTypes = types
                .Where(t => !typeof(Terminal).IsAssignableFrom(t) &&
                            !typeof(IEvent).IsAssignableFrom(t) &&
                            !t.ImplementsGenericInterface(typeof(IBinary<>)))
                .ToList();
            var regularTypes = entityTypes
                .Where(t => !typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            var wrapperTypes = entityTypes
                .Where(t => typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            var terminalTypes = types
                .Where(t => typeof(Terminal).IsAssignableFrom(t))
                .ToList();
            var binaryTypes = types
                .Where(t => t.ImplementsGenericInterface(typeof(IBinary<>)))
                .ToList();
            var eventTypes = types
                .Where(t => !t.IsAbstract && typeof(IEvent).IsAssignableFrom(t))
                .ToList();

            void ValidateCommon(Type type)
            {
                #region Check general stuff

                if (type.FullName == null)
                    throw new InvalidResourceDeclarationException(
                        "Encountered an unknown type. No further information is available.");

                if (type.IsGenericTypeDefinition)
                    throw new InvalidResourceDeclarationException(
                        $"Found a generic resource type '{type.GetRESTableTypeName()}'. RESTable resource types must be non-generic");

                if (type.FullName.Count(c => c == '+') >= 2)
                    throw new InvalidResourceDeclarationException($"Invalid resource '{type.GetRESTableTypeName()}'. " +
                                                                  "Inner resources cannot have their own inner resources");

                if (type.HasAttribute<RESTableViewAttribute>())
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource type '{type.GetRESTableTypeName()}'. Resource types cannot be " +
                        "decorated with the 'RESTableViewAttribute'");

                if (type.Namespace == null)
                    throw new InvalidResourceDeclarationException($"Invalid type '{type.GetRESTableTypeName()}'. Unknown namespace");

                if (Configuration.ReservedNamespaces.Contains(type.Namespace.ToLower()) &&
                    type.Assembly != typeof(RESTableConfigurator).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid namespace for resource type '{type.GetRESTableTypeName()}'. Namespace '{type.Namespace}' is reserved by RESTable");

                if ((!type.IsClass || !type.IsPublic && !type.IsNestedPublic) && type.Assembly != typeof(Resource).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid type '{type.GetRESTableTypeName()}'. Resource types must be public classes");

                if (type.GetRESTableInterfaceType() is Type interfaceType)
                {
                    if (!interfaceType.IsInterface)
                        throw new InvalidResourceDeclarationException(
                            $"Invalid Interface of type '{interfaceType.GetRESTableTypeName()}' assigned to resource '{type.GetRESTableTypeName()}'. " +
                            "Type is not an interface");

                    if (interfaceType.GetProperties()
                        .Select(p => p.Name)
                        .ContainsDuplicates(StringComparer.OrdinalIgnoreCase, out var interfacePropDupe))
                        throw new InvalidResourceMemberException(
                            $"Invalid Interface of type '{interfaceType.GetRESTableTypeName()}' assigned to resource '{type.GetRESTableTypeName()}'. " +
                            $"Interface contained properties with duplicate names matching '{interfacePropDupe}' (case insensitive).");

                    var interfaceName = interfaceType.GetRESTableTypeName();
                    type.GetInterfaceMap(interfaceType).TargetMethods.ForEach(method =>
                    {
                        if (!method.IsSpecialName) return;
                        var interfaceProperty = interfaceType
                            .GetProperties()
                            .First(p => p.GetGetMethod()?.Name is string getname && method.Name.EndsWith(getname) ||
                                        p.GetSetMethod()?.Name is string setname && method.Name.EndsWith(setname));

                        Type propertyType = null;
                        if (method.IsPrivate && method.Name.StartsWith($"{interfaceName}.get_") || method.Name.StartsWith("get_"))
                            propertyType = method.ReturnType;
                        else if (method.IsPrivate && method.Name.StartsWith($"{interfaceName}.set_") || method.Name.StartsWith("set_"))
                            propertyType = method.GetParameters()[0].ParameterType;

                        if (propertyType == null)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.GetRESTableTypeName()}' assigned to resource '{type.GetRESTableTypeName()}'. " +
                                $"Unable to determine the type for interface property '{interfaceProperty.Name}'");

                        PropertyInfo projectedProperty;
                        if (method.Name.StartsWith($"{interfaceName}.get_"))
                        {
                            projectedProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetGetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else if (method.Name.StartsWith($"{interfaceName}.set_"))
                        {
                            projectedProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetSetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else return;

                        if (projectedProperty == null)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.GetRESTableTypeName()}' assigned to resource '{type.GetRESTableTypeName()}'. " +
                                $"RESTable was unable to determine which property of '{type.GetRESTableTypeName()}' that is exposed by interface " +
                                $"property '{interfaceProperty.Name}'. For getters, RESTable will look for the last IL instruction " +
                                "in the method body that fetches a property value from the resource type. For setters, RESTable will look " +
                                "for the last IL instruction in the method body that sets a property value in the resource type.");

                        if (projectedProperty.PropertyType != propertyType)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.GetRESTableTypeName()}' assigned to resource '{type.GetRESTableTypeName()}'. " +
                                $"RESTable matched interface property '{interfaceProperty.Name}' with resource property '{projectedProperty.Name}' " +
                                "using the interface property matching rules, but these properties have a type mismatch. Expected " +
                                $"'{projectedProperty.PropertyType.GetRESTableTypeName()}' but found '{propertyType.GetRESTableTypeName()}' in interface");
                    });
                }

                #endregion

                #region Check for invalid IDictionary implementation

                var validTypes = new[] {typeof(string), typeof(object)};
                if (type.ImplementsGenericInterface(typeof(IDictionary<,>), out var typeParams)
                    && !type.IsSubclassOf(typeof(JObject))
                    && !typeParams.SequenceEqual(validTypes))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.GetRESTableTypeName()}'. All resource types implementing " +
                        "the generic 'System.Collections.Generic.IDictionary`2' interface must either be subclasses of " +
                        "Newtonsoft.Json.Linq.JObject or have System.String as first type parameter and System.Object as " +
                        $"second type parameter. Found {typeParams[0].GetRESTableTypeName()} and {typeParams[1].GetRESTableTypeName()}");

                #endregion

                #region Check for invalid IEnumerable implementation

                if ((type.ImplementsGenericInterface(typeof(IEnumerable<>)) || typeof(IEnumerable).IsAssignableFrom(type)) &&
                    !type.ImplementsGenericInterface(typeof(IDictionary<,>)))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.GetRESTableTypeName()}'. The type has an invalid imple" +
                        $"mentation of an IEnumerable interface. The resource '{type.GetRESTableTypeName()}' (or any of its base types) " +
                        "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                        "rable\' interfaces without also implementing the \'System.Collections.Generic.IDictionary`2\' interface."
                    );

                #endregion

                #region Check for public instance fields

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Any())
                    throw new InvalidResourceMemberException(
                        $"A RESTable resource cannot have public instance fields, only properties. Resource: '{type.GetRESTableTypeName()}' had " +
                        $"fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.GetRESTableTypeName()}'"
                    );

                #endregion

                #region Check for properties with duplicate case insensitive names

                if (TypeCache.FindAndParseDeclaredProperties(type).ContainsDuplicates(DeclaredProperty.NameComparer, out var duplicate))
                    throw new InvalidResourceMemberException(
                        $"Invalid properties for resource '{type.GetRESTableTypeName()}'. Names of public instance properties must " +
                        $"be unique (case insensitive). Two or more property names were equivalent to '{duplicate.Name}'."
                    );

                #endregion
            }

            void ValidateEntityDeclarations(List<Type> regularResources)
            {
                foreach (var type in regularResources)
                    ValidateCommon(type);
            }

            void ValidateWrapperDeclaration(List<Type> wrappers)
            {
                if (wrappers.Select(type => (type, wrapped: type.GetWrappedType())).ContainsDuplicates(pair => pair.wrapped, out var dupe))
                    throw new InvalidResourceWrapperException(dupe, "must wrap unique types. Found multiple wrapper declarations for " +
                                                                    $"wrapped type '{dupe.wrapped.GetRESTableTypeName()}'.");

                foreach (var wrapper in wrappers)
                {
                    var wrapped = wrapper.GetWrappedType();
                    var _types = (wrapper, wrapped);
                    var members = wrapper.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    if (members.OfType<PropertyInfo>().Any() || members.OfType<FieldInfo>().Any())
                        throw new InvalidResourceWrapperException(_types, "cannot contain public instance properties or fields");
                    ValidateCommon(wrapper);
                    if (wrapper.GetInterfaces()
                        .Where(i => typeof(IOperationsInterface).IsAssignableFrom(i))
                        .Any(i => i.IsGenericType && i.GenericTypeArguments[0] != wrapped))
                        throw new InvalidResourceWrapperException(_types, "cannot implement operations interfaces for types other than " +
                                                                          $"'{wrapped.GetRESTableTypeName()}'.");
                    if (wrapped.FullName?.Contains("+") == true)
                        throw new InvalidResourceWrapperException(_types, "cannot wrap types that are declared within the scope of some other class.");
                    if (wrapped.HasAttribute<RESTableAttribute>())
                        throw new InvalidResourceWrapperException(_types, "cannot wrap types already decorated with the 'RESTableAttribute' attribute");
                    if (wrapper.Assembly == typeof(RESTableConfigurator).Assembly)
                        throw new InvalidResourceWrapperException(_types, "cannot wrap RESTable types");
                }
            }

            void ValidateTerminalDeclarations(List<Type> terminals)
            {
                foreach (var terminal in terminals)
                {
                    ValidateCommon(terminal);

                    if (terminal.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidTerminalDeclarationException(terminal, "must not be collections");
                    if (terminal.HasResourceProviderAttribute())
                        throw new InvalidTerminalDeclarationException(terminal, "must not be decorated with a resource provider attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(terminal))
                        throw new InvalidTerminalDeclarationException(terminal, "must not implement any other RESTable operations interfaces");
                    if (terminal != null && terminal.GetConstructor(Type.EmptyTypes) == null)
                        throw new InvalidTerminalDeclarationException(terminal, "must define a public parameterless constructor");
                }
            }

            void ValidateBinaryDeclarations(List<Type> binaries)
            {
                foreach (var binary in binaries)
                {
                    ValidateCommon(binary);
                    if (binary.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidBinaryDeclarationException(binary, "must not be collections");
                    if (binary.HasResourceProviderAttribute())
                        throw new InvalidBinaryDeclarationException(binary, "must not be decorated with a resource provider attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(binary))
                        throw new InvalidBinaryDeclarationException(binary, "must not implement any other RESTable operations interfaces");
                }
            }

            void ValidateEventDeclarations(List<Type> events)
            {
                foreach (var @event in events)
                {
                    ValidateCommon(@event);
                    if (!typeof(IEvent).IsAssignableFrom(@event))
                        throw new InvalidEventDeclarationException(@event, "must inherit from 'RESTable.Resources.Event<T>'");
                    if (@event.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidEventDeclarationException(@event, "must not be collections");
                    if (@event.HasResourceProviderAttribute())
                        throw new InvalidEventDeclarationException(@event, "must not be decorated with a resource provider attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(@event))
                        throw new InvalidEventDeclarationException(@event, "must not implement any RESTable operations interfaces");
                }
            }

            ValidateEntityDeclarations(entityTypes);
            ValidateWrapperDeclaration(wrapperTypes);
            ValidateTerminalDeclarations(terminalTypes);
            ValidateBinaryDeclarations(binaryTypes);
            ValidateEventDeclarations(eventTypes);

            return (regularTypes, wrapperTypes, terminalTypes, binaryTypes, eventTypes);
        }
    }
}