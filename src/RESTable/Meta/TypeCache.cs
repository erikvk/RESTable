﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RESTable.Internal;
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
        private EntityTypeResolverController EntityTypeResolverController { get; }
        internal TermFactory TermFactory { get; }
        private ResourceCollection ResourceCollection { get; }

        public TypeCache
        (
            EntityTypeResolverController entityTypeResolverController,
            ResourceCollection resourceCollection,
            TermCache termCache
        )
        {
            EntityTypeResolverController = entityTypeResolverController;
            ResourceCollection = resourceCollection;
            DeclaredPropertyCache = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            DeclaredPropertyCacheByActualName = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
            EntityTypeContracts = new ConcurrentDictionary<Type, EntityTypeContract>();
            TermFactory = new TermFactory(this, termCache, resourceCollection);
        }

        #region Declared properties

        internal IEnumerable<DeclaredProperty> FindAndParseDeclaredProperties(Type type, bool flag = false)
        {
            if (type.HasAttribute<RESTableMemberAttribute>(out var memberAttribute) && memberAttribute.Ignored)
                return new DeclaredProperty[0];
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
                return value;
            GetDeclaredProperties(type);
            return EntityTypeContracts[type];
        }

        /// <summary>
        /// Gets the declared properties for a given type
        /// </summary>
        public IReadOnlyDictionary<string, DeclaredProperty> GetDeclaredProperties(Type type, bool groupByActualName = false)
        {
            IEnumerable<DeclaredProperty> Make(Type _type)
            {
                switch (_type)
                {
                    case null: return new DeclaredProperty[0];
                    case var _ when _type.IsInterface:
                    {
                        return ParseDeclaredProperties
                        (
                            props: new[] {_type}
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
                            .Where(group => @group.Key != null)
                            .ToDictionary(m => m.Key, m => (
                                getter: m.FirstOrDefault(p => p.GetParameters().Length == 0),
                                setter: m.FirstOrDefault(p => p.GetParameters().Length == 1)
                            ));
                        return Make(t).Select(p =>
                        {
                            var (getter, setter) = targetsByProp.SafeGet(p.ActualName);
                            if (p.IsReadable)
                            {
                                p.ActualName = getter.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && getter.IsSpecialName
                                        ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetGetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop != null)?
                                    .Name;
                            }
                            else if (p.IsWritable)
                            {
                                p.ActualName = setter.GetInstructions()
                                    .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && setter.IsSpecialName
                                        ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .FirstOrDefault(prop => prop.GetSetMethod() == calledMethod)
                                        : null)
                                    .LastOrDefault(prop => prop != null)?
                                    .Name;
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
                    default:
                    {
                        return FindAndParseDeclaredProperties(_type);
                    }
                }
            }

            if (type?.GetRESTableTypeName() == null) return null;

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
                    EntityTypeResolverController.InvokeContractResolvers(contract);
                    propsByName = DeclaredPropertyCache[type] = propertyList.SafeToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
                }
                return propsByName;
            }

            if (!DeclaredPropertyCacheByActualName.TryGetValue(type, out var propsByActualName))
            {
                propsByActualName = DeclaredPropertyCacheByActualName[type] = GetDeclaredProperties(type)
                    .Values
                    .SafeToDictionary(p => p.ActualName, StringComparer.OrdinalIgnoreCase);
            }
            return propsByActualName;
        }

        /// <summary>
        /// Gets the DeclaredProperty for a given PropertyInfo
        /// </summary>
        public DeclaredProperty GetDeclaredProperty(PropertyInfo member)
        {
            var declaringType = member.DeclaringType;
            if (declaringType.GetRESTableTypeName() == null)
                throw new Exception($"Cannot get declared property for member '{member}' of unknown type");
            GetDeclaredProperties(declaringType, true).TryGetValue(member.Name, out var property);
            return property;
        }


        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <returns></returns>
        public DeclaredProperty FindDeclaredProperty(Type type, string key)
        {
            var isDictionary = typeof(IDictionary).IsAssignableFrom(type) ||
                               type.ImplementsGenericInterface(typeof(IDictionary<,>));
            if (!isDictionary && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var elementType = type.ImplementsGenericInterface(typeof(IEnumerable<>), out var p)
                    ? p[0]
                    : typeof(object);
                var collectionReadonly = typeof(IList).IsAssignableFrom(type) || type.ImplementsGenericInterface(typeof(IList<>));
                switch (key)
                {
                    case "-": return new LastIndexProperty(elementType, collectionReadonly, type);
                    case var _ when int.TryParse(key, out var integer):
                        return new IndexProperty(integer, key, elementType, collectionReadonly, type);
                }
            }

            if (!GetDeclaredProperties(type).TryGetValue(key, out var prop))
            {
                if (type.IsNullable(out var underlying))
                    type = underlying;
                var resource = ResourceCollection.SafeGetResource(type);
                throw new UnknownProperty(type, resource, key);
            }
            return prop;
        }

        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <param name="declaredProperty">The declared property found</param>
        /// <returns></returns>
        public bool TryFindDeclaredProperty(Type type, string key, out DeclaredProperty declaredProperty)
        {
            return GetDeclaredProperties(type).TryGetValue(key, out declaredProperty);
        }
        
        internal void EstablishPropertyDependancies(DeclaredProperty property)
        {
            if (property.HasAttribute<DefinesAttribute>(out var dAttribute) && dAttribute.Terms is string[] dArgs && dArgs.Any())
            {
                foreach (var term in dArgs.Select(name => TermFactory.MakeOrGetCachedTerm(property.Owner, name, ".", TermBindingRule.OnlyDeclared)))
                    property.DefinesPropertyTerms.Add(term);
                property.DefinesOtherProperties = true;
            }
        }

        #endregion
    }
}