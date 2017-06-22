using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Starcounter;

namespace RESTar.Deflection
{
    public class StaticProperty : Property
    {
        public override string Name { get; protected set; }
        public override string DatabaseQueryName { get; protected set; }
        public Type Type { get; protected set; }
        public override bool Dynamic => false;
        public override bool ScQueryable { get; protected set; }
        public IEnumerable<Attribute> Attributes;

        internal TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute =>
            Attributes?.OfType<TAttribute>().FirstOrDefault();

        internal bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;

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

        protected StaticProperty(bool scQueryable) => ScQueryable = scQueryable;

        public static StaticProperty Parse(string keyString, Type resource) => resource.MatchProperty(keyString);
    }
}