using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using RESTar.Linq;
using static System.Reflection.BindingFlags;
using static System.StringComparer;
using static RESTar.Deflection.Dynamic.SpecialProperty;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Deflection.Dynamic
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their declared properties.
    /// </summary>limit
    public static class TypeCache
    {
        static TypeCache()
        {
            DeclaredPropertyCache = new ConcurrentDictionary<Type, IDictionary<string, DeclaredProperty>>();
            TermCache = new ConcurrentDictionary<(string, string, TermBindingRules), Term>();
        }

        #region Terms

        internal static readonly ConcurrentDictionary<(string Type, string Key, TermBindingRules BindingRule), Term> TermCache;

        /// <summary>
        /// Condition terms are terms that refer to properties in resources, or  for
        /// use in conditions.
        /// </summary>
        internal static Term MakeConditionTerm(this ITarget target, string key) =>
            target.Type.MakeOrGetCachedTerm(key, target.ConditionBindingRule);

        /// <summary>
        /// Output terms are terms that refer to properties in RESTar output. If they refer to
        /// a property in the dynamic domain, they are not cached. 
        /// </summary>
        internal static Term MakeOutputTerm(this IResource target, string key, ICollection<string> dynamicDomain) =>
            dynamicDomain == null
                ? MakeOrGetCachedTerm(target.Type, key, target.OutputBindingRule)
                : Term.Parse(target.Type, key, target.OutputBindingRule, dynamicDomain);

        internal static Term MakeOrGetCachedTerm(this Type resource, string key, TermBindingRules bindingRule)
        {
            var tuple = (resource.FullName, key.ToLower(), bindingRule);
            if (!TermCache.TryGetValue(tuple, out var term))
                term = TermCache[tuple] = Term.Parse(resource, key, bindingRule, null);
            return term;
        }

        internal static void ClearTermsFor<T>() => TermCache
            .Where(pair => pair.Key.Type == typeof(T).FullName)
            .Select(pair => pair.Key)
            .ToList()
            .ForEach(key => TermCache.TryRemove(key, out var _));

        #endregion

        #region Declared properties

        private static readonly ConcurrentDictionary<Type, IDictionary<string, DeclaredProperty>> DeclaredPropertyCache;

        private static IEnumerable<DeclaredProperty> ParseDeclaredProperties(this IEnumerable<PropertyInfo> props, bool flag) => props
            .Where(p => !p.RESTarIgnored())
            .Where(p => !p.GetIndexParameters().Any())
            .Select(p => new DeclaredProperty(p, flag))
            .OrderBy(p => p.Order);

        /// <summary>
        /// Gets the declared properties for a given type
        /// </summary>
        public static IDictionary<string, DeclaredProperty> GetDeclaredProperties(this Type type)
        {
            IEnumerable<DeclaredProperty> make(Type _type)
            {
                switch (_type)
                {
                    case null: return new DeclaredProperty[0];
                    case var _ when _type.IsNullable(out var underlying):
                        return underlying.GetDeclaredProperties().Values;
                    case var _ when _type.HasAttribute<RESTarViewAttribute>():
                        return _type.GetProperties(Instance | Public)
                            .ParseDeclaredProperties(false)
                            .Union(make(_type.DeclaringType));
                    case var _ when _type.IsDDictionary():
                        return _type.GetProperties(Instance | Public)
                            .ParseDeclaredProperties(flag: true)
                            .Union(GetObjectIDAndObjectNo(flag: true));
                    case var _ when Resource.SafeGet(_type)?.DeclaredPropertiesFlagged == true:
                        return _type.GetProperties(Instance | Public)
                            .ParseDeclaredProperties(flag: true);
                    case var _ when _type.IsInterface:
                        return new[] {_type}
                            .Concat(_type.GetInterfaces())
                            .SelectMany(i => i.GetProperties(Instance | Public))
                            .ParseDeclaredProperties(false);
                    default:
                        return _type.GetProperties(Instance | Public)
                            .ParseDeclaredProperties(false)
                            .If(_type.IsStarcounter, ps => ps.Union(GetObjectIDAndObjectNo(false)));
                }
            }

            if (type?.FullName == null) return null;
            if (!DeclaredPropertyCache.TryGetValue(type, out var props))
                props = DeclaredPropertyCache[type] = make(type).ToDictionary(p => p.Name, OrdinalIgnoreCase);
            return props;
        }

        /// <summary>
        /// Gets the DeclaredProperty for a given MemberInfo
        /// </summary>
        public static DeclaredProperty GetDeclaredProperty(this MemberInfo member)
        {
            var declaringType = member.DeclaringType;
            if (declaringType?.FullName == null)
                throw new Exception($"Cannot get declared property for member '{member}' of unknown type");
            return declaringType.GetDeclaredProperties().FirstOrDefault(p => p.Value.ActualName == member.Name).Value;
        }

        #endregion
    }
}