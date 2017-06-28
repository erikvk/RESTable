using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        static TypeCache() => StaticProperties = new Dictionary<Type, IEnumerable<StaticProperty>>();

        private static readonly IDictionary<Type, IEnumerable<StaticProperty>> StaticProperties;

        /// <summary>
        /// Gets the static properties for a given type
        /// </summary>
        public static IEnumerable<StaticProperty> GetStaticProperties(this Type type) =>
            StaticProperties.ContainsKey(type)
                ? StaticProperties[type]
                : (StaticProperties[type] = FindStaticProperties(type));

        /// <summary>
        /// Gets the static properties for a given resource
        /// </summary>
        public static IEnumerable<StaticProperty> GetStaticProperties(this IResource resource) =>
            GetStaticProperties(resource.TargetType);

        /// <summary>
        /// Gets the table columns for a Starcounter database resource
        /// </summary>
        public static IEnumerable<StaticProperty> GetTableColumns(this IResource resource)
        {
            if (!resource.IsStarcounterResource)
                throw new Exception($"Cannot get table columns for non-starcounter resource '{resource.Name}'");
            return resource.TargetType.GetProperties(Instance | Public).Select(p => new StaticProperty(p));
        }

        private static IEnumerable<StaticProperty> FindStaticProperties(Type type)
        {
            if (type.IsDDictionary())
                return new[] {ObjectNo, ObjectID};
            var declared = type.GetProperties(Instance | Public)
                .Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>())
                .Select(p => new StaticProperty(p))
                .ToList();
            if (type.IsStarcounter())
            {
                declared.Add(ObjectNo);
                declared.Add(ObjectID);
            }
            return declared;
        }
    }
}