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
        public Type Type { get; }

        /// <inheritdoc />
        public override bool Dynamic => false;

        /// <summary>
        /// Automatically sets the Skip property of conditions matched against this property to true
        /// </summary>
        public bool ConditionSkip { get; }

        /// <summary>
        /// Hidden properties are not included in regular output, but can be added and queried on.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// The order at which this property appears when all properties are enumerated
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>  
        public ICollection<Attribute> Attributes { get; }

        /// <summary>
        /// Gets the first instance of a given attribute type that this resource property 
        /// has been decorated with.
        /// </summary>
        public T GetAttribute<T>() where T : Attribute => Attributes?.OfType<T>().FirstOrDefault();

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute =>
            (attribute = GetAttribute<TAttribute>()) != null;

        /// <summary>
        /// Used in SpecialProperty
        /// </summary>
        protected StaticProperty(string name, string databaseQueryName, Type type, int? order, bool scQueryable,
            ICollection<Attribute> attributes, bool conditionSkip, bool hidden, Getter getter, Setter setter)
        {
            Name = name;
            DatabaseQueryName = databaseQueryName;
            Type = type;
            Order = order;
            ScQueryable = scQueryable;
            Attributes = attributes;
            ConditionSkip = conditionSkip;
            Hidden = hidden;
            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// The regular constructor
        /// </summary>
        internal StaticProperty(PropertyInfo p, bool flagName = false)
        {
            if (p == null) return;
            Name = p.RESTarMemberName();
            if (flagName) Name = "$" + Name;
            DatabaseQueryName = p.Name;
            Type = p.PropertyType;
            Order = p.GetOrder();
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                          p.PropertyType.IsStarcounterCompatible();
            Attributes = p.GetCustomAttributes().ToList();
            ConditionSkip = p.ShouldSkipConditions() ||
                            p.DeclaringType.HasAttribute<RESTarViewAttribute>();
            Hidden = p.ShouldBeHidden();
            Getter = p.MakeDynamicGetter();
            Setter = p.MakeDynamicSetter();
        }

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
                case var _ when this.ShouldFlattenExcelToString():
                case var _ when Type.IsClass: return (typeof(string), true);
                case var _ when Type.IsNullable(out var baseType): return (baseType, true);
                default: return (Type, false);
            }
        }

        internal void WriteCell(DataRow row, object target)
        {
            object baseValue = Type.IsEnum || this.ShouldFlattenExcelToString()
                ? GetValue(target)?.ToString()
                : GetValue(target);
            row[Name] = baseValue.MakeDynamicCellValue();
        }
    }
}