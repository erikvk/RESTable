using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using RESTar.Linq;
using static System.Reflection.BindingFlags;
using static RESTar.Deflection.Dynamic.SpecialProperty;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Deflection.Dynamic
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their static properties.
    /// </summary>limit
    public static class TypeCache
    {
        static TypeCache()
        {
            StaticPropertyCache = new ConcurrentDictionary<string, IDictionary<string, StaticProperty>>();
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

        #region Static properties

        private static readonly ConcurrentDictionary<string, IDictionary<string, StaticProperty>> StaticPropertyCache;

        private static IEnumerable<StaticProperty> ParseStaticProperties(this IEnumerable<PropertyInfo> props, bool flag) => props
            .Where(p => !p.ShouldBeIgnored())
            .Where(p => !p.GetIndexParameters().Any())
            .Select(p => new StaticProperty(p, flag))
            .OrderBy(p => p.Order);

        /// <summary>
        /// Gets the static properties for a given type
        /// </summary>
        public static IDictionary<string, StaticProperty> GetStaticProperties(this Type type)
        {
            IEnumerable<StaticProperty> make(Type _type)
            {
                switch (_type)
                {
                    case null: return new StaticProperty[0];
                    case var _ when _type.HasAttribute<RESTarViewAttribute>():
                        return _type.GetProperties(Instance | Public)
                            .ParseStaticProperties(false)
                            .Union(make(_type.DeclaringType));
                    case var _ when _type.IsDDictionary():
                        return _type.GetProperties(Instance | Public)
                            .ParseStaticProperties(flag: true)
                            .Union(GetObjectIDAndObjectNo(flag: true));
                    case var _ when Resource.SafeGet(_type)?.StaticPropertiesFlagged == true:
                        return _type.GetProperties(Instance | Public)
                            .ParseStaticProperties(flag: true);
                    case var _ when _type.IsInterface:
                        return new[] {_type}
                            .Concat(_type.GetInterfaces())
                            .SelectMany(i => i.GetProperties(Instance | Public))
                            .ParseStaticProperties(false);
                    default:
                        return _type.GetProperties(Instance | Public)
                            .ParseStaticProperties(false)
                            .If(_type.IsStarcounter, ps => ps.Union(GetObjectIDAndObjectNo(false)));
                }
            }

            if (type?.FullName == null) return null;
            if (!StaticPropertyCache.TryGetValue(type.FullName, out var props))
                props = StaticPropertyCache[type.FullName] = make(type).ToDictionary(p => p.Name.ToLower());
            return props;
        }

        #endregion
    }
}