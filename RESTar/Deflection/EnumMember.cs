using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RESTar.Deflection
{
    /// <summary>
    /// A struct to describe a member of an enumeration
    /// </summary>
    public struct EnumMember<T>
    {
        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers() => GetMembers(null);

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers(T except) => GetMembers(new[] {except});

        /// <summary>
        /// Gets the members of an enumeration
        /// </summary>
        public static EnumMember<T>[] GetMembers(IEnumerable<T> except)
        {
            var exceptionStrings = except?.Select(e => e.ToString()).ToList();
            return typeof(T).IsEnum
                ? typeof(T).GetFields()
                    .Where(t => t.FieldType.IsEnum)
                    .Where(t => exceptionStrings?.Contains(t.Name) == false)
                    .Select(t => new EnumMember<T>
                    (
                        attributes: t.GetCustomAttributes<Attribute>(),
                        name: t.Name,
                        value: (int) (Convert.ChangeType(t.GetValue(null), TypeCode.Int32) ?? -1)
                    ))
                    .ToArray()
                : throw new ArgumentException($"Type must be enum, found '{typeof(T).FullName}'");
        }

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
        public bool HasAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().Any();

        /// <summary>
        /// Returns an attribute for an enumeration, or null if there is no such
        /// attribute decoration for this member
        /// </summary>
        public T1 GetAttribute<T1>() where T1 : Attribute => Attributes.OfType<T1>().FirstOrDefault();
    }
}