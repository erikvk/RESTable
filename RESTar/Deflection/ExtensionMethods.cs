using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static List<EnumMember> GetEnumMembers(this Type type) => type.IsEnum
            ? type.GetFields()
                .Where(t => t.FieldType.IsEnum)
                .Select(t => new EnumMember
                {
                    Attributes = t.GetCustomAttributes<Attribute>(),
                    Name = t.Name,
                    Value = (int) (Convert.ChangeType(t.GetValue(null), TypeCode.Int32) ?? -1)
                })
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
            public IEnumerable<Attribute> Attributes;

            /// <summary>
            /// The name of the enumeration members
            /// </summary>
            public string Name;

            /// <summary>
            /// The integer value of the enumeration
            /// </summary>
            public int Value;

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