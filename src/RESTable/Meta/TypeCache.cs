using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RESTable.Resources;
using RESTable.Meta.IL;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Meta
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their declared properties.
    /// </summary>limit
    public class TypeCache
    {
        internal ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCache { get; }
        private ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCacheByActualName { get; }
        private ConcurrentDictionary<Type, EntityTypeContract> EntityTypeContracts { get; }
        private IEnumerable<IEntityTypeContractResolver> EntityTypeContractResolvers { get; }
        internal TermFactory TermFactory { get; }
        private ResourceCollection ResourceCollection { get; }

        public TypeCache
        (
            IEnumerable<IEntityTypeContractResolver> entityTypeContractResolvers,
            ResourceCollection resourceCollection,
            TermCache termCache
        )
        {
            EntityTypeContractResolvers = entityTypeContractResolvers;
            ResourceCollection = resourceCollection;
            DeclaredPropertyCache = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            DeclaredPropertyCacheByActualName = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            EntityTypeContracts = new ConcurrentDictionary<Type, EntityTypeContract>();
            TermFactory = new TermFactory(this, termCache, resourceCollection);
        }

        #region Declared properties

        internal IEnumerable<DeclaredProperty> FindAndParseDeclaredProperties(Type type, bool flag = false)
        {
            if (type.HasAttribute<RESTableMemberAttribute>(out var memberAttribute) && memberAttribute!.Ignored)
                return Array.Empty<DeclaredProperty>();
            return ParseDeclaredProperties(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), flag);
        }

        internal IEnumerable<DeclaredProperty> ParseDeclaredProperties(IEnumerable<PropertyInfo> props, bool flag) => props
            .Where(p => !p.RESTableIgnored())
            .Where(p => !p.GetIndexParameters().Any())
            .Select(p => new DeclaredProperty(p, flag))
            .OrderBy(p => p.Order);

        public EntityTypeContract GetEntityTypeContract(Type type)
        {
            if (EntityTypeContracts.TryGetValue(type, out var value))
                return value!;
            GetDeclaredProperties(type);
            return EntityTypeContracts[type];
        }

        /// <summary>
        /// Gets the declared properties for a given type
        /// </summary>
        public IReadOnlyDictionary<string, DeclaredProperty> GetDeclaredProperties(Type? type, bool groupByActualName = false)
        {
            IEnumerable<DeclaredProperty> Make(Type? _type)
            {
                switch (_type)
                {
                    case null: return Array.Empty<DeclaredProperty>();
                    case var _ when _type.IsInterface:
                    {
                        return ParseDeclaredProperties
                        (
                            props: new[] { _type }
                                .Concat(_type.GetInterfaces())
                                .SelectMany(i => i.GetProperties(BindingFlags.Instance | BindingFlags.Public)),
                            flag: false
                        );
                    }
                    case var _ when _type.GetRESTableInterfaceType() is Type t:
                    {
                        var interfaceName = t.GetRESTableTypeName();
                        var targetsByProp = _type
                            .GetInterfaceMap(t)
                            .TargetMethods
                            .GroupBy(m =>
                            {
                                if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.get_"))
                                    return m.Name.Split(interfaceName + ".get_")[1];
                                if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.set_"))
                                    return m.Name.Split(interfaceName + ".set_")[1];
                                if (m.Name.StartsWith("get_"))
                                    return m.Name.Split("get_")[1];
                                if (m.Name.StartsWith("set_"))
                                    return m.Name.Split("set_")[1];
                                return null;
                            })
                            .Where(group => @group.Key is not null)
                            .ToDictionary(m => m.Key!, m => (
                                getter: m.FirstOrDefault(p => p.GetParameters().Length == 0),
                                setter: m.FirstOrDefault(p => p.GetParameters().Length == 1)
                            ));
                        return Make(t).Select(p =>
                        {
                            var (getter, setter) = targetsByProp.SafeGet(p.ActualName);
                            if (p.IsReadable)
                            {
                                p.ActualName = getter!.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && getter!.IsSpecialName
                                        ? type!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetGetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop is not null)?
                                    .Name!;
                            }
                            else if (p.IsWritable)
                            {
                                p.ActualName = setter!.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && setter!.IsSpecialName
                                        ? type!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetSetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop is not null)?
                                    .Name!;
                            }
                            return p;
                        });
                    }
                    case var _ when _type.IsSubclassOf(typeof(Terminal)):
                    {
                        return FindAndParseDeclaredProperties(_type).Except(GetDeclaredProperties(typeof(Terminal)).Values, DeclaredProperty.NameComparer);
                    }
                    case var _ when _type.IsNullable(out var underlying):
                    {
                        return GetDeclaredProperties(underlying).Values;
                    }
                    case var _ when _type.HasAttribute<RESTableViewAttribute>():
                    {
                        return FindAndParseDeclaredProperties(_type).Union(Make(_type.DeclaringType));
                    }
                    case var _ when _type.IsDictionary():
                    {
                        return FindAndParseDeclaredProperties(_type, flag: true).Select(p =>
                        {
                            p.Hidden = true;
                            return p;
                        });
                    }
                    default:
                    {
                        return FindAndParseDeclaredProperties(_type);
                    }
                }
            }

            if (type?.GetRESTableTypeName() is null)
                throw new Exception("Could not get declared properties for unknown type");

            if (!groupByActualName)
            {
                if (!DeclaredPropertyCache.TryGetValue(type, out var propsByName))
                {
                    var propertyList = new List<DeclaredProperty>();
                    foreach (var property in Make(type))
                    {
                        EstablishPropertyDependancies(property);
                        propertyList.Add(property);
                    }
                    var contract = EntityTypeContracts[type] = new EntityTypeContract(type, propertyList);
                    foreach (var resolver in EntityTypeContractResolvers)
                        resolver.ResolveContract(contract);
                    propsByName = DeclaredPropertyCache[type] = propertyList.SafeToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                return propsByName!;
            }

            if (!DeclaredPropertyCacheByActualName.TryGetValue(type, out var propsByActualName))
            {
                propsByActualName = DeclaredPropertyCacheByActualName[type] = GetDeclaredProperties(type)
                    .Values
                    .SafeToDictionary(p => p.ActualName, StringComparer.OrdinalIgnoreCase);
            }
            return propsByActualName!;
        }

        public DeclaredProperty FindDeclaredProperty(Type type, string key)
        {
            if (TryFindDeclaredProperty(type, key, out var property))
            {
                return property!;
            }
            if (type.IsNullable(out var underlying))
                type = underlying!;
            var resource = ResourceCollection.GetResource(type!);
            throw new UnknownProperty(type, resource, key);
        }

        public bool TryFindDeclaredProperty(Type type, string key, out DeclaredProperty? declaredProperty)
        {
            if (!IsDictionary(type) && ImplementsEnumerableInterface(type, out var parameter))
            {
                var collectionReadonly = typeof(IList).IsAssignableFrom(type) || type.ImplementsGenericInterface(typeof(IList<>));
                switch (key)
                {
                    case "-":
                    {
                        declaredProperty = new LastIndexProperty(parameter!, collectionReadonly, type);
                        return true;
                    }
                    case var _ when int.TryParse(key, out var integer):
                    {
                        declaredProperty = new IndexProperty(integer, key, parameter!, collectionReadonly, type);
                        return true;
                    }
                    default:
                    {
                        declaredProperty = null;
                        return false;
                    }
                }
            }

            if (GetDeclaredProperties(type).TryGetValue(key, out var prop))
            {
                declaredProperty = prop!;
                return true;
            }
            declaredProperty = null;
            return false;
        }

        private static bool IsDictionary(Type type) => typeof(IDictionary).IsAssignableFrom(type) ||
                                                       type.ImplementsGenericInterface(typeof(IDictionary<,>));

        private static bool ImplementsEnumerableInterface(Type type, out Type? parameter)
        {
            if (type.ImplementsGenericInterface(typeof(IEnumerable<>), out var parameters) ||
                type.ImplementsGenericInterface(typeof(IAsyncEnumerable<>), out parameters))
            {
                parameter = parameters![0];
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                parameter = typeof(object);
                return true;
            }
            parameter = null;
            return false;
        }

        internal void EstablishPropertyDependancies(DeclaredProperty property)
        {
            if (property.HasAttribute<DefinesAttribute>(out var dAttribute) && dAttribute!.Terms is string[] dArgs && dArgs.Any())
            {
                foreach (var term in dArgs.Select(name => TermFactory.MakeOrGetCachedTerm(property.Owner!, name, ".", TermBindingRule.OnlyDeclared)))
                    property.DefinesPropertyTerms.Add(term);
                property.DefinesOtherProperties = true;
            }
        }

        #endregion
    }
}