using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Starcounter;
using static Newtonsoft.Json.NullValueHandling;

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
        /// The order at which this property appears when all properties are enumerated
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// Hidden properties are not included in regular output, but can be added and queried on.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Should this property be hidden in output if the value is null? Only applies to JSON output.
        /// </summary>
        public bool HiddenIfNull { get; }

        /// <summary>
        /// Automatically sets the Skip property of conditions matched against this property to true
        /// </summary>
        public bool SkipConditions { get; }

        /// <summary>
        /// Should the serializer flatten this property using the ToString() method when writing to excel?
        /// </summary>
        public bool ExcelFlattenToString { get; }

        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>  
        private ICollection<Attribute> Attributes { get; }

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
            ICollection<Attribute> attributes, bool skipConditions, bool hidden, bool hiddenIfNull, bool excelFlattenToString,
            Operators allowedConditionOperators, Getter getter, Setter setter)
        {
            Name = name;
            DatabaseQueryName = databaseQueryName;
            Type = type;
            Order = order;
            ScQueryable = scQueryable;
            Attributes = attributes;
            SkipConditions = skipConditions;
            Hidden = hidden;
            HiddenIfNull = hiddenIfNull;
            ExcelFlattenToString = excelFlattenToString;
            AllowedConditionOperators = allowedConditionOperators;
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
            Attributes = p.GetCustomAttributes().ToList();

            var attribute = GetAttribute<RESTarMemberAttribute>();
            Order = attribute?.Order ?? GetAttribute<JsonPropertyAttribute>()?.Order;
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                          p.PropertyType.IsStarcounterCompatible();
            SkipConditions = attribute?.SkipConditions == true ||
                             p.DeclaringType.HasAttribute<RESTarViewAttribute>();
            Hidden = attribute?.Hidden == true;
            HiddenIfNull = attribute?.HiddenIfNull == true || GetAttribute<JsonPropertyAttribute>()?.NullValueHandling == Ignore;
            ExcelFlattenToString = attribute?.ExcelFlattenToString == true;
            AllowedConditionOperators = attribute?.AllowedOperators ?? Operators.All;

            Getter = p.MakeDynamicGetter();
            if (attribute?.ReadOnly != true)
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
                case var _ when ExcelFlattenToString:
                case var _ when Type.IsClass: return (typeof(string), true);
                case var _ when Type.IsNullable(out var baseType): return (baseType, true);
                default: return (Type, false);
            }
        }

        internal void WriteCell(DataRow row, object target)
        {
            object baseValue = Type.IsEnum || ExcelFlattenToString
                ? GetValue(target)?.ToString()
                : GetValue(target);
            row[Name] = baseValue.MakeDynamicCellValue();
        }
    }
}