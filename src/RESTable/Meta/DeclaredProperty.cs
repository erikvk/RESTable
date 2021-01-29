using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.Meta
{
    /// <summary>
    /// Represents the operation to attach to a PropertyChanged event handler in DeclaredProperty
    /// </summary>
    public delegate void PropertyChangeHandler
    (
        DeclaredProperty property,
        object target,
        dynamic oldValue,
        dynamic newValue
    );

    /// <inheritdoc />
    /// <summary>
    /// A declared property represents a compile time known property of a type.
    /// </summary>
    public class DeclaredProperty : Property
    {
        /// <summary>
        /// A unique identifier for this property
        /// </summary>
        private int MetadataToken { get; }

        /// <inheritdoc />
        public override bool IsDynamic => false;

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
        /// Does this declared property represent a datetime?
        /// </summary>
        public bool IsDateTime { get; }

        /// <summary>
        /// A custom datetime format string of this property (if any)
        /// </summary>
        public string CustomDateTimeFormat { get; }

        /// <summary>
        /// Should this member, and all its members, be merged onto the owner type when (de)serializing?
        /// </summary>
        public bool MergeOntoOwner { get; }

        /// <summary>
        /// The type that this property was declared in
        /// </summary>
        public Type Owner { get; }

        /// <summary>
        /// Does the value of this property define the values of other properties?
        /// </summary>
        public bool DefinesOtherProperties { get; private set; }

        private ISet<Term> definesPropertyTerms;

        /// <summary>
        /// The properties that reflects upon this property
        /// </summary>
        public ISet<Term> DefinesPropertyTerms => definesPropertyTerms ??= new HashSet<Term>();

        /// <summary>
        /// An event that is fired when this property is changed for some target
        /// </summary>
        public event PropertyChangeHandler PropertyChanged;

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
        public bool HasAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute => (attribute = GetAttribute<TAttribute>()) != null;

        /// <inheritdoc />
        public override void SetValue(object target, dynamic value)
        {
            if (PropertyChanged != null)
            {
                var oldValue = GetValue(target);
                base.SetValue(target, (object) value);
                var changedValue = GetValue(target);
                if (object.Equals(changedValue, oldValue))
                    return;
                NotifyChange(target, oldValue, value);
                return;
            }
            base.SetValue(target, (object) value);
        }

        internal void NotifyChange(object target, object oldValue, object newValue)
        {
            PropertyChanged?.Invoke(this, target, oldValue, newValue);
        }

        /// <summary>
        /// Used in SpecialProperty
        /// </summary>
        protected DeclaredProperty
        (
            int metadataToken,
            string name,
            string actualName,
            Type type,
            int? order,
            ICollection<Attribute> attributes,
            bool skipConditions,
            bool hidden,
            bool hiddenIfNull,
            bool isEnum,
            string customDateTimeFormat,
            Operators allowedConditionOperators,
            Type owner,
            Getter getter,
            Setter setter
        )
        {
            MetadataToken = metadataToken;
            Name = name;
            Type = type;
            ActualName = actualName;
            Order = order;
            Attributes = attributes;
            SkipConditions = skipConditions;
            Hidden = hidden;
            HiddenIfNull = hiddenIfNull;
            IsEnum = isEnum;
            AllowedConditionOperators = allowedConditionOperators;
            IsNullable = !type.IsValueType || type.IsNullable(out _) || hidden;
            CustomDateTimeFormat = customDateTimeFormat;
            IsDateTime = type == typeof(DateTime) || type == typeof(DateTime?);
            Getter = getter;
            Setter = setter;
            Owner = owner;
            MergeOntoOwner = false;
        }

        /// <summary>
        /// The regular constructor, called by the type cache when creating declared properties
        /// </summary>
        internal DeclaredProperty(PropertyInfo p, bool flagName = false)
        {
            if (p == null) return;

            MetadataToken = p.MetadataToken;
            Name = p.RESTableMemberName(flagName);
            Type = p.PropertyType;
            ActualName = p.Name;
            Attributes = p.GetCustomAttributes().ToList();
            var memberAttribute = GetAttribute<RESTableMemberAttribute>();
            var jsonAttribute = GetAttribute<JsonPropertyAttribute>();
            CustomDateTimeFormat = memberAttribute?.DateTimeFormat;
            MergeOntoOwner = memberAttribute?.MergeOntoOwner ?? false;
            Order = memberAttribute?.Order ?? jsonAttribute?.Order;

            SkipConditions = memberAttribute?.SkipConditions == true || p.DeclaringType.HasAttribute<RESTableViewAttribute>();
            Hidden = memberAttribute?.Hidden == true;
            HiddenIfNull = memberAttribute?.HiddenIfNull == true || jsonAttribute?.NullValueHandling == NullValueHandling.Ignore;
            AllowedConditionOperators = memberAttribute?.AllowedOperators ?? Operators.All;
            IsNullable = !p.PropertyType.IsValueType || p.PropertyType.IsNullable(out _) || Hidden;
            IsEnum = p.PropertyType.IsEnum || p.PropertyType.IsNullable(out var @base) && @base.IsEnum;
            IsDateTime = p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?);
            if (memberAttribute?.ExcelReducerName != null)
                ExcelReducer = MakeExcelReducer(memberAttribute.ExcelReducerName, p);
            Getter = p.MakeDynamicGetter();
            if (memberAttribute?.ReadOnly != true)
                Setter = p.MakeDynamicSetter();
            ReplaceOnUpdate = memberAttribute?.ReplaceOnUpdate == true;
            Owner = p.DeclaringType;
        }

        internal void EstablishPropertyDependancies()
        {
            // if (HasAttribute<DefinedByAttribute>(out var dbAttribute) && dbAttribute.Terms is string[] dbArgs && dbArgs.Any())
            // {
            //     foreach (var definingTerm in dbArgs.Select(name => Owner.MakeOrGetCachedTerm(name, ".", TermBindingRule.OnlyDeclared)))
            //     {
            //         var definer = definingTerm.LastAs<DeclaredProperty>();
            //         definer.DefinesOtherProperties = true;
            //         definer.DefinesPropertyTerms.Add(definingTerm);
            //     }
            // }
            if (HasAttribute<DefinesAttribute>(out var dAttribute) && dAttribute.Terms is string[] dArgs && dArgs.Any())
            {
                foreach (var term in dArgs.Select(name => Owner.MakeOrGetCachedTerm(name, ".", TermBindingRule.OnlyDeclared)))
                    DefinesPropertyTerms.Add(term);
                DefinesOtherProperties = true;
            }
        }

        private static dynamic MakeExcelReducer(string methodName, PropertyInfo p)
        {
            if (p.DeclaringType == null) throw new Exception("Type error, cannot cache property " + p);
            try
            {
                var method = p.DeclaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception();
                return method.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, typeof(string)));
            }
            catch
            {
                throw new Exception($"Invalid or unknown excel reduce function '{methodName}' for property '{p.Name}' in type '" +
                                    $"{p.DeclaringType.GetRESTableTypeName()}'. Must be public instance method with signature 'public " +
                                    "string <insert-name-here>()'");
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
            var isDictionary = typeof(IDictionary).IsAssignableFrom(type) ||
                               type.ImplementsGenericInterface(typeof(IDictionary<,>));
            if (!isDictionary && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var elementType = type.ImplementsGenericInterface(typeof(IEnumerable<>), out var p)
                    ? p[0]
                    : typeof(object);
                var collectionReadonly = typeof(IList).IsAssignableFrom(type) || type.ImplementsGenericInterface(typeof(IList<>));
                switch (key)
                {
                    case "-": return new LastIndexProperty(elementType, collectionReadonly, type);
                    case var _ when int.TryParse(key, out var integer):
                        return new IndexProperty(integer, key, elementType, collectionReadonly, type);
                }
            }

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
                case byte[] binary: return binary.Length;
                default: return Type.CountBytes();
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{Type.GetRESTableTypeName()}.{Name}";

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is DeclaredProperty p && p.MetadataToken == MetadataToken;

        /// <inheritdoc />
        public override int GetHashCode() => MetadataToken;

        /// <summary>
        /// Compares properties by name
        /// </summary>
        public static readonly IEqualityComparer<DeclaredProperty> NameComparer = new _NameComparer();

        /// <summary>
        /// Compares properties by identity
        /// </summary>
        public static IEqualityComparer<DeclaredProperty> IdentityComparer = new _IdentityComparer();

        private class _NameComparer : IEqualityComparer<DeclaredProperty>
        {
            public bool Equals(DeclaredProperty x, DeclaredProperty y) => x?.Name == y?.Name;
            public int GetHashCode(DeclaredProperty obj) => obj.Name.GetHashCode();
        }

        private class _IdentityComparer : IEqualityComparer<DeclaredProperty>
        {
            public bool Equals(DeclaredProperty x, DeclaredProperty y) => x?.MetadataToken == y?.MetadataToken;
            public int GetHashCode(DeclaredProperty obj) => obj.MetadataToken;
        }
    }
}