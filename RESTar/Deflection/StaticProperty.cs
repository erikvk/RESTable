using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Starcounter;

namespace RESTar.Deflection
{
    /// <summary>
    /// A static property represents a compile time known property of a class.
    /// </summary>
    public class StaticProperty : Property
    {
        /// <summary>
        /// The property type for this property
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// Is this property dynamic?
        /// </summary>
        public override bool Dynamic => false;

        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>
        public IEnumerable<Attribute> Attributes { get; }

        internal T GetAttribute<T>() where T : Attribute => Attributes?.OfType<T>().FirstOrDefault();
        internal bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;
        internal StaticProperty(bool scQueryable) => ScQueryable = scQueryable;

        /// <summary>
        /// Parses a static property from a key string and a type
        /// </summary>
        /// <param name="keyString">The string to match a property from</param>
        /// <param name="type">The type to match the property from</param>
        /// <returns></returns>
        public static StaticProperty Parse(string keyString, Type type) => type.MatchProperty(keyString);

        internal StaticProperty(PropertyInfo p)
        {
            if (p == null) return;
            Name = p.MemberName();
            DatabaseQueryName = p.Name;
            Type = p.PropertyType;
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                          p.PropertyType.IsStarcounterCompatible();
            Attributes = p.GetCustomAttributes();
            Getter = p.MakeDynamicGetter();
            Setter = p.MakeDynamicSetter();
        }

        internal long ByteCount(object target)
        {
            if (target == null)
                throw new NullReferenceException(nameof(target));
            object value = Get(target);
            switch (value)
            {
                case null: return 0;
                case string s: return Encoding.UTF8.GetByteCount(s);
                case Binary binary: return binary.ToArray().Length;
                default: return CountBytes(Type);
            }
        }

        private static long CountBytes(Type type)
        {
            if (type.IsEnum) return 8;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return CountBytes(type.GenericTypeArguments[0]);
                    if (type.IsStarcounter()) return 16;
                    throw new Exception($"Unknown type encountered: '{type.FullName}'");
                case TypeCode.Boolean: return 4;
                case TypeCode.Char: return 2;
                case TypeCode.SByte: return 1;
                case TypeCode.Byte: return 1;
                case TypeCode.Int16: return 2;
                case TypeCode.UInt16: return 2;
                case TypeCode.Int32: return 4;
                case TypeCode.UInt32: return 4;
                case TypeCode.Int64: return 8;
                case TypeCode.UInt64: return 8;
                case TypeCode.Single: return 4;
                case TypeCode.Double: return 8;
                case TypeCode.Decimal: return 16;
                case TypeCode.DateTime: return 8;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}