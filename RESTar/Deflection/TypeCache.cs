using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using RESTar.Internal;
using static System.Reflection.BindingFlags;
using static RESTar.Deflection.SpecialProperty;

namespace RESTar.Deflection
{
    /// <summary>
    /// The type cache keeps track of discovered types and provides
    /// fast access to their static properties.
    /// </summary>
    public static class TypeCache
    {
        static TypeCache()
        {
            StaticProperties = new ConcurrentDictionary<string, IDictionary<string, StaticProperty>>();
            PropertyChains = new ConcurrentDictionary<int, PropertyChain>();
        }

        private static readonly ConcurrentDictionary<string, IDictionary<string, StaticProperty>> StaticProperties;
        internal static readonly ConcurrentDictionary<int, PropertyChain> PropertyChains;

        #region Static properties

        /// <summary>
        /// Gets the static properties for a given resource
        /// </summary>
        public static IDictionary<string, StaticProperty> GetStaticProperties(this IResource resource) =>
            GetStaticProperties(resource.TargetType);

        /// <summary>
        /// Gets the static properties for a given type
        /// </summary>
        public static IDictionary<string, StaticProperty> GetStaticProperties(this Type type)
        {
            if (StaticProperties.TryGetValue(type.FullName, out IDictionary<string, StaticProperty> props))
                return props;
            return StaticProperties[type.FullName] = type.IsDDictionary()
                ? new StaticProperty[] {ObjectNo, ObjectID}.ToDictionary(p => p.Name.ToLower(), p => p)
                : type.GetProperties(Instance | Public)
                    .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                    .Where(p => !(p.DeclaringType?.GetInterface("IDictionary`2") != null && p.Name == "Item"))
                    .Select(p => new StaticProperty(p))
                    .If(type.IsStarcounter, list => list.Union(new[] {ObjectNo, ObjectID}))
                    .OrderBy(p => p.GetAttribute<JsonPropertyAttribute>()?.Order)
                    .ToList()
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
            return resource.TargetType.GetProperties(Instance | Public).Select(p => new StaticProperty(p));
        }

        #endregion
    }
}