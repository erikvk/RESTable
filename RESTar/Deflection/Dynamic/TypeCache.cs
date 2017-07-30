using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Linq;
using static System.Reflection.BindingFlags;
using static RESTar.Deflection.Dynamic.SpecialProperty;

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
            TermCache = new ConcurrentDictionary<int, Term>();
        }

        private static readonly ConcurrentDictionary<string, IDictionary<string, StaticProperty>> StaticPropertyCache;
        internal static readonly ConcurrentDictionary<int, Term> TermCache;

        #region Static properties

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
            if (StaticPropertyCache.TryGetValue(type.FullName, out var props))
                return props;
            return StaticPropertyCache[type.FullName] = type.IsDDictionary()
                ? new StaticProperty[] {ObjectNo, ObjectID}.ToDictionary(p => p.Name.ToLower(), p => p)
                : type.GetProperties(Instance | Public)
                    .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                    .Where(p => !(p.DeclaringType.Implements(typeof(IDictionary<,>)) && p.Name == "Item"))
                    .Select(p => new StaticProperty(p))
                    .If(type.IsStarcounter, then: list => list.Union(new[] {ObjectNo, ObjectID}))
                    .OrderBy(p => p.GetAttribute<JsonPropertyAttribute>()?.Order)
                    .ToDictionary(p => p.Name.ToLower(), p => p);
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

        #endregion
    }
}