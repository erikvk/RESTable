using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using RESTar.Linq;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.Deflection.Dynamic.SpecialProperty;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Deflection.Dynamic
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their static properties.
    /// </summary>
    public static class TypeCache
    {
        static TypeCache()
        {
            StaticPropertyCache = new ConcurrentDictionary<string, IDictionary<string, StaticProperty>>();
            TermCache = new ConcurrentDictionary<(string, string, bool), Term>();
        }

        #region Terms

        internal static readonly ConcurrentDictionary<(string Resource, string Key, bool DynUnknowns), Term> TermCache;

        /// <summary>
        /// Converts a PropertyInfo to a Term
        /// </summary>
        public static Term ToTerm(this PropertyInfo propertyInfo)
        {
            return propertyInfo.DeclaringType.MakeTerm(propertyInfo.Name,
                Resource.SafeGet(propertyInfo.DeclaringType)?.DynamicConditionsAllowed == true);
        }

        internal static Term MakeTerm(this IResource resource, string key, bool dynamicUnknowns)
        {
            return resource.Type.MakeTerm(key, dynamicUnknowns);
        }

        internal static Term MakeTerm(this Type resource, string key, bool dynamicUnknowns)
        {
            var tuple = (resource.FullName, key.ToLower(), dynamicUnknowns);
            if (!TermCache.TryGetValue(tuple, out var term))
                term = TermCache[tuple] = Term.Parse(resource, key, dynamicUnknowns);
            return term;
        }

        internal static void ClearTermsFor<T>() => TermCache
            .Where(pair => pair.Key.Resource == typeof(T).FullName)
            .Select(pair => pair.Key)
            .ToList()
            .ForEach(key => TermCache.TryRemove(key, out var _));

        #endregion

        #region Static properties

        internal static readonly ConcurrentDictionary<string, IDictionary<string, StaticProperty>> StaticPropertyCache;

        /// <summary>
        /// Gets the static properties for a given resource
        /// </summary>
        public static IDictionary<string, StaticProperty> GetStaticProperties(this IResource resource) =>
            GetStaticProperties(resource.Type);

        /// <summary>
        /// Gets the static properties for a given type
        /// </summary>
        public static IDictionary<string, StaticProperty> GetStaticProperties(this Type type)
        {
            if (type.FullName == null) return null;
            if (StaticPropertyCache.TryGetValue(type.FullName, out var props))
                return props;
            return StaticPropertyCache[type.FullName] = type.IsDDictionary()
                ? new StaticProperty[] {ObjectNo, ObjectID}.ToDictionary(p => p.Name.ToLower())
                : (type.IsInterface
                    ? new[] {type}.Concat(type.GetInterfaces()).SelectMany(i => i.GetProperties())
                    : type.GetProperties(Instance | Public))
                .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                .Where(p => !p.GetIndexParameters().Any())
                .Select(p => new StaticProperty(p))
                .If(type.IsStarcounter, then: list => list.Union(new[] {ObjectNo, ObjectID}))
                .OrderBy(p => p.GetAttribute<JsonPropertyAttribute>()?.Order)
                .ToDictionary(p => p.Name.ToLower());
        }

        #endregion

        #region Table columns

        /// <summary>
        /// Gets the table columns for a Starcounter database resource
        /// </summary>
        public static IEnumerable<StaticProperty> GetTableColumns(this IResource resource)
        {
            if (!resource.IsStarcounterResource)
                throw new Exception($"Cannot get table columns for non-starcounter resource '{resource.Name}'");
            return resource.Type.GetProperties(Instance | Public)
                .Select(p => new StaticProperty(p)).ToList();
        }

        /// <summary>
        /// Gets the table columns for a Starcounter database resource
        /// </summary>
        internal static IEnumerable<StaticProperty> GetTableColumns(this Type resource)
        {
            if (!resource.HasAttribute<DatabaseAttribute>())
                throw new Exception($"Cannot get table columns for non-starcounter resource '{resource.Name}'");
            return resource.GetProperties(Instance | Public)
                .Select(p => new StaticProperty(p)).ToList();
        }

        #endregion
    }
}