using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RESTar.Results.Fail.NotFound;
using Starcounter;
using static System.Reflection.BindingFlags;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// A declared property represents a compile time known property of a type.
    /// </summary>
    public class DeclaredProperty : Property
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
        /// The function to use when reduding this property to an Excel-compatible string
        /// </summary>
        public dynamic ExcelReducer { get; }

        /// <summary>
        /// Should this object be replaced with a new instance on update, or reused? Applicable for types 
        /// such as Dictionaries and Lists.
        /// </summary>
        public bool ReplaceOnUpdate { get; }

        /// <summary>
        /// Is this property nullable?
        /// </summary>
        public bool Nullable { get; }

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
        internal DeclaredProperty(string name, string actualName, Type type, int? order, bool scQueryable, ICollection<Attribute> attributes,
            bool skipConditions, bool hidden, bool hiddenIfNull, Operators allowedConditionOperators, Getter getter, Setter setter)
        {
            Name = name;
            ActualName = actualName;
            Type = type;
            Order = order;
            ScQueryable = scQueryable;
            Attributes = attributes;
            SkipConditions = skipConditions;
            Hidden = hidden;
            HiddenIfNull = hiddenIfNull;
            AllowedConditionOperators = allowedConditionOperators;
            Nullable = type.IsClass || type.IsNullable(out var _);
            Getter = getter;
            Setter = setter;
        }

        /// <summary>
        /// The regular constructor, called by the type cache when creating declared properties
        /// </summary>
        internal DeclaredProperty(PropertyInfo p, bool flagName = false)
        {
            if (p == null) return;
            Name = p.RESTarMemberName(flagName);
            ActualName = p.Name;
            Type = p.PropertyType;
            Attributes = p.GetCustomAttributes().ToList();
            var memberAttribute = GetAttribute<RESTarMemberAttribute>();
            var jsonAttribute = GetAttribute<JsonPropertyAttribute>();
            Order = memberAttribute?.Order ?? jsonAttribute?.Order;
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true && p.PropertyType.IsStarcounterCompatible();
            SkipConditions = memberAttribute?.SkipConditions == true || p.DeclaringType.HasAttribute<RESTarViewAttribute>();
            Hidden = memberAttribute?.Hidden == true;
            HiddenIfNull = memberAttribute?.HiddenIfNull == true || jsonAttribute?.NullValueHandling == Ignore;
            AllowedConditionOperators = memberAttribute?.AllowedOperators ?? Operators.All;
            Nullable = p.PropertyType.IsClass || p.PropertyType.IsNullable(out var _);
            if (memberAttribute?.ExcelReducerName != null)
                ExcelReducer = MakeExcelReducer(memberAttribute.ExcelReducerName, p);
            Getter = p.MakeDynamicGetter();
            if (memberAttribute?.ReadOnly != true)
                Setter = p.MakeDynamicSetter();
            ReplaceOnUpdate = memberAttribute?.ReplaceOnUpdate == true;
        }

        private static dynamic MakeExcelReducer(string methodName, PropertyInfo p)
        {
            if (p.DeclaringType == null) throw new Exception("Type error, cannot cache property " + p);
            try
            {
                var method = p.DeclaringType.GetMethod(methodName, Public | Instance) ?? throw new Exception();
                return method.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, typeof(string)));
            }
            catch
            {
                throw new Exception($"Invalid or unknown excel reduce function '{methodName}' for property '{p.Name}' in type '" +
                                    $"{p.DeclaringType.FullName}'. Must be public instance method with signature 'public string <name>()'");
            }
        }

        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <returns></returns>
        public static DeclaredProperty Find(Type type, string key)
        {
            if (!type.GetDeclaredProperties().TryGetValue(key, out var prop))
            {
                if (type.IsNullable(out var underlying))
                    type = underlying;
                throw new UnknownProperty(type, key);
            }
            return prop;
        }

        /// <summary>
        /// Parses a declared property from a key string and a type
        /// </summary>
        /// <param name="type">The type to match the property from</param>
        /// <param name="key">The string to match a property from</param>
        /// <param name="declaredProperty">The declared property found</param>
        /// <returns></returns>
        public static bool TryFind(Type type, string key, out DeclaredProperty declaredProperty)
        {
            return type.GetDeclaredProperties().TryGetValue(key, out declaredProperty);
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
                case var _ when ExcelReducer != null:
                case var _ when Type.IsClass: return (typeof(string), true);
                case var _ when Type.IsNullable(out var baseType): return (baseType, true);
                default: return (Type, false);
            }
        }

        internal void WriteCell(DataRow row, object target)
        {
            object getBaseValue()
            {
                switch (this)
                {
                    case var _ when ExcelReducer != null: return ExcelReducer((dynamic) target);
                    case var _ when Type.IsEnum: return GetValue(target)?.ToString();
                    default: return GetValue(target);
                }
            }

            row[Name] = getBaseValue().MakeDynamicCellValue();
        }
    }
}