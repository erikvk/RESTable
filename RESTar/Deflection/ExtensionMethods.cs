using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;

namespace RESTar.Deflection
{
    /// <summary>
    /// Extension methods for deflection operations
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Makes a fast delegate for getting the value for a given property.
        /// </summary>
        public static Getter MakeDynamicGetter(this PropertyInfo p)
        {
            try
            {
                if (p.DeclaringType?.IsValueType == true)
                    return p.GetValue;
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

        /// <summary>
        /// Makes a fast delegate for setting the value for a given property.
        /// </summary>
        public static Setter MakeDynamicSetter(this PropertyInfo p)
        {
            try
            {
                if (p.DeclaringType?.IsValueType == true)
                    return p.SetValue;
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

        internal static Func<object, string, string> MakeKeyMatcher(this Type type)
        {
            if (typeof(JObject).IsAssignableFrom(type))
                return (t, str) => ((JObject) t).MatchKeyIgnoreCase(str);
            if (typeof(DDictionary).IsAssignableFrom(type))
                return (t, str) => ((DDictionary) t).MatchKeyIgnoreCase(str);
            if (typeof(Dictionary<string, dynamic>).IsAssignableFrom(type))
                return (t, str) => ((Dictionary<string, dynamic>) t).MatchKeyIgnoreCase(str);
            if (typeof(IDictionary).IsAssignableFrom(type))
                return (t, str) => ((IDictionary) t).MatchKeyIgnoreCase_IDict(str);
            throw new Exception("Unknown dictionary type: " + type.FullName);
        }

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static ICollection<EnumMember> GetEnumMembers(this Type type) => type.IsEnum
            ? type.GetFields()
                .Where(t => t.FieldType.IsEnum)
                .Select(t => new EnumMember
                (
                    attributes: t.GetCustomAttributes<Attribute>(),
                    name: t.Name,
                    value: (int) (Convert.ChangeType(t.GetValue(null), TypeCode.Int32) ?? -1)
                ))
                .ToList()
            : throw new ArgumentException("Must be enum", nameof(type));

        /// <summary>
        /// A struct to describe a member of an enumeration
        /// </summary>
        public struct EnumMember
        {
            /// <summary>
            /// The attributes of the enumeration member
            /// </summary>
            public readonly IEnumerable<Attribute> Attributes;

            /// <summary>
            /// The name of the enumeration members
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The integer value of the enumeration
            /// </summary>
            public readonly int Value;

            /// <summary>
            ///  </summary>
            internal EnumMember(IEnumerable<Attribute> attributes, string name, int value)
            {
                Attributes = attributes;
                Name = name;
                Value = value;
            }

            /// <summary>
            /// Returns true if and only if the enumeration member has an attribute
            /// of the given attribute type
            /// </summary>
            public bool HasAttribute<T>() where T : Attribute => Attributes.OfType<T>().Any();

            /// <summary>
            /// Returns an attribute for an enumeration, or null if there is no such
            /// attribute decoration for this member
            /// </summary>
            public T GetAttribute<T>() where T : Attribute => Attributes.OfType<T>().FirstOrDefault();
        }
    }
}