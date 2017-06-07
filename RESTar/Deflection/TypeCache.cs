using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using static RESTar.Deflection.SpecialProperty;
using static System.StringComparison;

namespace RESTar.Deflection
{
    public static class TypeCache
    {
        private static readonly IDictionary<Type, IEnumerable<StaticProperty>> StaticProperties;
        static TypeCache() => StaticProperties = new Dictionary<Type, IEnumerable<StaticProperty>>();

        public static IEnumerable<StaticProperty> GetStaticProperties(this Type type) =>
            StaticProperties.ContainsKey(type)
                ? StaticProperties[type]
                : (StaticProperties[type] = FindStaticProperties(type));

        private static IEnumerable<StaticProperty> FindStaticProperties(Type type)
        {
            if (type.IsDDictionary())
                return new[] {ObjectNo, ObjectID};
            var declared = type.GetProperties()
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

    public static class DeflectionExtensionMethods
    {
        public static Getter MakeDynamicGetter(this PropertyInfo p)
        {
            try
            {
                var getterDelegate = p
                    .GetGetMethod()?
                    .CreateDelegate(typeof(Func<,>)
                        .MakeGenericType(p.DeclaringType, p.PropertyType));
                return getterDelegate != null ? obj => ((dynamic) getterDelegate)((dynamic) obj) : default(Getter);
            }
            catch
            {
                return null;
            }
        }

        public static Setter MakeDynamicSetter(this PropertyInfo p)
        {
            try
            {
                var setterDelegate = p
                    .GetSetMethod()?
                    .CreateDelegate(typeof(Action<,>)
                        .MakeGenericType(p.DeclaringType, p.PropertyType));
                return setterDelegate != null
                    ? (obj, value) => ((dynamic) setterDelegate)((dynamic) obj, value)
                    : default(Setter);
            }
            catch
            {
                return null;
            }
        }

        internal static StaticProperty MatchProperty(this Type resource, string str, bool ignoreCase = true)
        {
            var matches = resource.GetStaticProperties()
                .Where(p => string.Equals(str, p.Name, ignoreCase
                    ? CurrentCultureIgnoreCase
                    : CurrentCulture));
            var count = matches.Count();
            if (count == 0) throw new UnknownColumnException(resource, str);
            if (count > 1)
            {
                if (!ignoreCase)
                    throw new AmbiguousColumnException(resource, str, matches.Select(m => m.Name));
                return MatchProperty(resource, str, false);
            }
            return matches.First();
        }
    }
}