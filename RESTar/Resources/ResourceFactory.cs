using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Admin;
using RESTar.Internal;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    internal static class ResourceFactory
    {
        internal static DDictionaryProvider DDictProvider { get; }
        internal static StarcounterProvider ScProvider { get; }
        internal static VirtualResourceProvider VrProvider { get; }
        internal static DynamicResourceProvider DynProvider { get; }
        private static List<ResourceProvider> ResourceProviders { get; }

        static ResourceFactory()
        {
            ScProvider = new StarcounterProvider();
            DDictProvider = new DDictionaryProvider {DatabaseIndexer = ScProvider.DatabaseIndexer};
            VrProvider = new VirtualResourceProvider();
            DynProvider = new DynamicResourceProvider();
            ResourceProviders = new List<ResourceProvider> {DDictProvider, ScProvider, VrProvider};
        }

        private static void ValidateResourceProviders(ICollection<ResourceProvider> externalProviders)
        {
            if (externalProviders == null) return;
            externalProviders.ForEach(p => p.Validate());
            if (externalProviders.ContainsDuplicates(p => p.GetType().FullName, out var typeDupe))
                throw new ExternalResourceProviderException("Two or more external ResourceProviders with the same " +
                                                            $"type '{typeDupe.GetType().FullName}' was found. Include " +
                                                            "only one in the call to RESTarConfig.Init()");
            if (externalProviders.Select(p => p.GetProviderId().ToLower()).ContainsDuplicates(out var idDupe))
                throw new ExternalResourceProviderException("Two or more external ResourceProviders had simliar type " +
                                                            "names, which would lead to confusion. Only one provider " +
                                                            $"should be associated with '{idDupe}'");
            ResourceProviders.AddRange(externalProviders);
        }

        /// <summary>
        /// All types covered by RESTar are selected and validated here
        /// </summary>
        /// <returns></returns>
        private static (List<Type>, List<Type>) ValidateAndBuildTypeLists()
        {
            (List<Type> regular, List<Type> wrappers) lists;
            var allTypes = typeof(object).GetSubclasses().ToList();
            var resourceTypes = allTypes.Where(t => t.HasAttribute<RESTarAttribute>()).ToList();
            var viewTypes = allTypes.Where(t => t.HasAttribute<RESTarViewAttribute>()).ToList();

            void ValidateResourceTypes(List<Type> types)
            {
                if (types.ContainsDuplicates(t => t.FullName?.ToLower() ?? "unknown", out var dupe))
                    throw new ResourceDeclarationException("RESTar resources must have unique case insensitive names. Found " +
                                                           $"multiple resource declarations for types with case insensitive name '{dupe}'.");
                var regularResourceTypes = types
                    .Where(t => !typeof(IResourceWrapper).IsAssignableFrom(t))
                    .ToList();
                var resourceWrapperTypes = types
                    .Where(t => typeof(IResourceWrapper).IsAssignableFrom(t))
                    .ToList();

                void ValidateCommon(Type type)
                {
                    #region Check general stuff

                    if (type.FullName == null)
                        throw new ResourceDeclarationException(
                            "Encountered an unknown type. No further information is available.");

                    if (type.FullName.Count(c => c == '+') >= 2)
                        throw new ResourceDeclarationException($"Invalid resource '{type.FullName.Replace('+', '.')}'. " +
                                                               "Inner resources cannot have their own inner resources");

                    if (type.HasAttribute<RESTarViewAttribute>())
                        throw new ResourceDeclarationException(
                            $"Invalid resource type '{type.FullName}'. Resource types cannot be " +
                            "decorated with the 'RESTarViewAttribute'");
                    if (type.Namespace == null)
                        throw new ResourceDeclarationException($"Invalid type '{type.FullName}'. Unknown namespace");

                    if (RESTarConfig.ReservedNamespaces.Contains(type.Namespace.ToLower()) &&
                        type.Assembly != typeof(RESTarConfig).Assembly)
                        throw new ResourceDeclarationException(
                            $"Invalid namespace for resource type '{type.FullName}'. Namespace '{type.Namespace}' is reserved by RESTar");

                    if ((!type.IsClass || !type.IsPublic && !type.IsNestedPublic) && type.Assembly != typeof(Resource).Assembly)
                        throw new ResourceDeclarationException(
                            $"Invalid type '{type.FullName}'. Resource types must be public classes");

                    #endregion

                    #region Check for invalid IDictionary implementation

                    var validTypes = new[] {typeof(string), typeof(object)};
                    if (type.Implements(typeof(IDictionary<,>), out var typeParams) && !typeParams.SequenceEqual(validTypes))
                        throw new ResourceDeclarationException(
                            $"Invalid resource declaration for type '{type.FullName}'. All resources implementing " +
                            "the generic 'System.Collections.Generic.IDictionary`2' interface must have System.String as " +
                            $"first type parameter and System.Object as second type parameter. Found {typeParams[0].FullName} " +
                            $"and {typeParams[1].FullName}");

                    #endregion

                    #region Check for invalid IEnumerable implementation

                    if ((type.Implements(typeof(IEnumerable<>)) || type.Implements(typeof(IEnumerable))) &&
                        !type.Implements(typeof(IDictionary<,>)))
                        throw new ResourceDeclarationException(
                            $"Invalid resource declaration for type '{type.FullName}'. The type has an invalid imple" +
                            $"mentation of an IEnumerable interface. The resource '{type.FullName}' (or any of its base types) " +
                            "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                            "rable\' interfaces without also implementing the \'System.Collections.Generic.IDictionary`2\' interface."
                        );

                    #endregion

                    #region Check for public instance fields

                    var fields = type.GetFields(Public | Instance);
                    if (fields.Any())
                        throw new ResourceMemberException(
                            $"A RESTar resource cannot have public instance fields, only properties. Resource: '{type.FullName}' had " +
                            $"fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.FullName}'"
                        );

                    #endregion

                    #region Check for properties with duplicate case insensitive names

                    if (type.GetProperties(Public | Instance)
                        .Where(p => !p.RESTarIgnored())
                        .Where(p => !(p.DeclaringType.Implements(typeof(IDictionary<,>)) && p.Name == "Item"))
                        .Select(p => p.RESTarMemberName().ToLower())
                        .ContainsDuplicates(out var duplicate))
                        throw new ResourceMemberException(
                            $"Invalid properties for resource '{type.FullName}'. Names of public instance properties declared " +
                            $"for a virtual resource must be unique (case insensitive). Two or more property names evaluated to {duplicate}."
                        );

                    #endregion
                }

                void ValidateRegularResourceDeclarations(List<Type> regularResources)
                {
                    foreach (var type in regularResources)
                        ValidateCommon(type);
                }

                void ValidateWrapperDeclaration(List<Type> wrappers)
                {
                    if (wrappers.Select(w => w.GetWrappedType()).ContainsDuplicates(out var wrapperDupe))
                        throw new ResourceWrapperException("RESTar found multiple RESTar.ResourceWrapper declarations for " +
                                                           $"type '{wrapperDupe.FullName}'. A type can only be wrapped once.");
                    foreach (var wrapper in wrappers)
                    {
                        var members = wrapper.GetMembers(Public | Instance);
                        if (members.OfType<PropertyInfo>().Any() || members.OfType<FieldInfo>().Any())
                            throw new ResourceWrapperException(
                                $"Invalid RESTar.ResourceWrapper '{wrapper.FullName}'. ResourceWrapper " +
                                "classes cannot contain public instance properties or fields");
                        ValidateCommon(wrapper);
                        var wrapped = wrapper.GetWrappedType();
                        if (wrapped.HasResourceProviderAttribute())
                            throw new ResourceWrapperException(
                                $"Invalid RESTar.ResourceWrapper '{wrapper.FullName}' for wrapped " +
                                $"type '{wrapped.FullName}'. Type decorated with a resource provider's " +
                                "attribute cannot be wrapped. Resource provider attributes should be " +
                                "placed on the wrapper type.");
                        if (wrapper.GetInterfaces()
                            .Where(i => typeof(IOperationsInterface).IsAssignableFrom(i))
                            .Any(i => i.IsGenericType && i.GenericTypeArguments[0] != wrapped))
                            throw new ResourceWrapperException(
                                $"Invalid RESTar.ResourceWrapper '{wrapper.FullName}'. This wrapper " +
                                "cannot implement operations interfaces for types other than " +
                                $"'{wrapped.FullName}'.");
                        if (wrapped.FullName?.Contains("+") == true)
                            throw new ResourceWrapperException($"Invalid RESTar.ResourceWrapper '{wrapper.FullName}'. Cannot " +
                                                               "wrap types that are declared within the scope of some other class.");
                        if (wrapped.HasAttribute<RESTarAttribute>())
                            throw new ResourceWrapperException("RESTar found a RESTar.ResourceWrapper declaration for type " +
                                                               $"'{wrapped.FullName}', a type that is already a RESTar " +
                                                               "resource type. Only non-resource types can be wrapped.");
                        if (wrapper.Namespace == null)
                            throw new ResourceDeclarationException($"Invalid type '{wrapper.FullName}'. Unknown namespace");
                        if (wrapper.Assembly == typeof(RESTarConfig).Assembly)
                            throw new ResourceWrapperException("RESTar found an invalid RESTar.ResourceWrapper declaration for " +
                                                               $"type '{wrapped.FullName}'. RESTar types cannot be wrapped.");
                    }
                }

                ValidateRegularResourceDeclarations(regularResourceTypes);
                ValidateWrapperDeclaration(resourceWrapperTypes);

                lists.regular = regularResourceTypes;
                lists.wrappers = resourceWrapperTypes;
            }

            void ValidateViewTypes(List<Type> types)
            {
                if (types.ContainsDuplicates(t => t.FullName?.ToLower() ?? "unknown", out var typeDupe))
                    throw new ResourceViewDeclarationException(typeDupe,
                        "RESTar resources must have unique case insensitive names. Found " +
                        $"multiple resource declarations for types with case insensitive name '{typeDupe.FullName}'.");
                foreach (var type in types)
                {
                    var resource = type.DeclaringType;
                    if (!type.IsClass || !type.IsNestedPublic || resource == null)
                        throw new ResourceViewDeclarationException(type,
                            "Resource view types must be declared as public classes nested within the the " +
                            "resource type they are views for");
                    if (type.IsSubclassOf(type))
                        throw new ResourceViewDeclarationException(type, "Views cannot inherit from their resource types");
                    if (!type.Implements(typeof(ISelector<>), out var param) || param[0] != resource)
                        throw new ResourceViewDeclarationException(type,
                            $"Expected view type to implement ISelector<{resource.FullName}>");
                    var propertyUnion = resource.GetProperties(Public | Instance).Union(type.GetProperties(Public | Instance));
                    if (propertyUnion.ContainsDuplicates(p => p.Name.ToLower(), out var propDupe))
                        throw new ResourceViewDeclarationException(type,
                            $"Invalid property '{propDupe.Name}'. Resource view types must not contain any public instance " +
                            "properties with the same name (case insensitive) as a property of the corresponding resource. " +
                            "All properties in the resource are automatically inherited for use in conditions with the view.");
                }
            }

            ValidateResourceTypes(resourceTypes);
            ValidateViewTypes(viewTypes);
            return lists;
        }

        private static void ValidateInnerResources() => RESTarConfig.Resources
            .GroupBy(r => r.ParentResourceName)
            .Where(group => group.Key != null)
            .ForEach(group =>
            {
                var parentResource = (IResourceInternal) Resource.SafeGet(group.Key);
                if (parentResource == null)
                    throw new ResourceDeclarationException(
                        $"Resource types {string.Join(", ", group.Select(item => $"'{item.Name}'"))} are declared " +
                        $"within the scope of another class '{group.Key}', that is not a RESTar resource. Inner " +
                        "resources must be declared within a resource class.");
                parentResource.InnerResources = group.ToList();
            });

        internal static void MakeResources(ICollection<ResourceProvider> externalProviders)
        {
            ValidateResourceProviders(externalProviders);
            var (regularResources, resourceWrappers) = ValidateAndBuildTypeLists();
            foreach (var provider in ResourceProviders)
            {
                var claim = provider.GetClaim(regularResources);
                regularResources = regularResources.Except(claim).ToList();
                provider.MakeClaimRegular(claim);
            }

            foreach (var provider in ResourceProviders)
            {
                var claim = provider.GetClaim(resourceWrappers);
                resourceWrappers = resourceWrappers.Except(claim).ToList();
                provider.MakeClaimWrapped(claim);
            }

            ValidateInnerResources();
            foreach (var provider in ResourceProviders)
            {
                if (provider.DatabaseIndexer != null)
                    DatabaseIndex.Indexers[provider.GetProviderId()] = provider.DatabaseIndexer;
                provider.ReceiveClaimed(Resource.ClaimedBy(provider));
            }

            DynamicResource.GetAll().ForEach(MakeDynamicResource);
        }

        internal static void MakeDynamicResource(DynamicResource resource) => DynProvider.BuildDynamicResource(resource);
    }
}