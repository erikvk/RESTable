using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Starcounter;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// A static property represents a compile time known property of a class.
    /// </summary>
    public class StaticProperty : Property
    {
        /// <summary>
        /// The property type for this property
        /// </summary>
        public Type Type { get; protected set; }

        /// <inheritdoc />
        public override bool Dynamic => false;

        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>  
        public ICollection<Attribute> Attributes { get; protected set; }

        internal T GetAttribute<T>() where T : Attribute => Attributes?.OfType<T>().FirstOrDefault();
        internal bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;
        internal StaticProperty(bool scQueryable) => ScQueryable = scQueryable;

        /// <summary>
        /// Parses a static property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <returns></returns>
        public static StaticProperty Find(Type type, string key)
        {
            type.GetStaticProperties().TryGetValue(key.ToLower(), out var prop);
            return prop ?? throw new UnknownPropertyException(type, key);
        }

        /// <summary>
        /// Parses a static property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <param name="staticProperty">The static property found</param>
        /// <returns></returns>
        public static bool TryFind(Type type, string key, out StaticProperty staticProperty)
        {
            return type.GetStaticProperties().TryGetValue(key.ToLower(), out staticProperty);
        }

        internal StaticProperty(PropertyInfo p)
        {
            if (p == null) return;
            Name = p.RESTarMemberName();
            DatabaseQueryName = p.Name;
            Type = p.PropertyType;
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                          p.PropertyType.IsStarcounterCompatible();
            Attributes = p.GetCustomAttributes().ToList();
            Getter = p.MakeDynamicGetter();
            Setter = p.MakeDynamicSetter();
        }

        internal long ByteCount(object target)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            switch (GetValue(target))
            {
                case null: return 0;
                case string str: return Encoding.UTF8.GetByteCount(str);
                case Binary binary: return binary.ToArray().Length;
                default: return Type.CountBytes();
            }
        }

        internal DataColumn MakeColumn()
        {
            var (type, nullable) = GetColumnSpec();
            return new DataColumn(Name, type) {AllowDBNull = nullable};
        }

        private (Type, bool) GetColumnSpec()
        {
            switch (Type)
            {
                case var _ when Type.IsEnum:
                case var _ when HasAttribute<ExcelFlattenToStringAttribute>():
                case var _ when Type.IsClass: return (typeof(string), true);
                case var _ when Type.IsNullable(out var baseType): return (baseType, true);
                default: return (Type, false);
            }
        }

        internal void WriteCell(DataRow row, object target)
        {
            object baseValue = Type.IsEnum || HasAttribute<ExcelFlattenToStringAttribute>()
                ? GetValue(target)?.ToString()
                : GetValue(target);
            row[Name] = baseValue.MakeDynamicCellValue();
        }
    }
}