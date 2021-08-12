using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A declared property represents a compile time known property of a type.
    /// </summary>
    public class DeclaredProperty : Property
    {
        /// <inheritdoc />
        public sealed override string Name { get; internal set; }

        /// <inheritdoc />
        public sealed override string ActualName { get; internal set; }

        /// <inheritdoc />
        public sealed override Type Type { get; protected set; }

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
        public bool Hidden { get; internal set; }

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
        public object? ExcelReducer { get; }

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
        /// Should this member, and all its members, be merged onto the owner type when (de)serializing?
        /// </summary>
        public bool MergeOntoOwner { get; }

        /// <summary>
        /// Does this property type implement the generic ICollection{T} interface?
        /// </summary>
        public bool IsCollection { get; }

        /// <summary>
        /// Is this property of a value type?
        /// </summary>
        public bool IsValueType { get; }

        /// <summary>
        /// Does the value of this property define the values of other properties?
        /// </summary>
        public bool DefinesOtherProperties { get; internal set; }

        private ISet<Term>? definesPropertyTerms;

        /// <summary>
        /// The properties that reflects upon this property
        /// </summary>
        public ISet<Term> DefinesPropertyTerms => definesPropertyTerms ??= new HashSet<Term>();

        /// <summary>
        /// An event that is fired when this property is changed for some target
        /// </summary>
        public event PropertyChangeHandler? PropertyChanged;

        /// <summary>
        /// The attributes that this property has been decorated with
        /// </summary>  
        private ICollection<Attribute> Attributes { get; }

        /// <summary>
        /// Gets the first instance of a given attribute type that this resource property 
        /// has been decorated with.
        /// </summary>
        public T? GetAttribute<T>() where T : Attribute => Attributes.OfType<T>().FirstOrDefault();

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() is not null;

        /// <summary>
        /// Returns true if and only if this resource property has been decorated with the given 
        /// attribute type.
        /// </summary>
        public bool HasAttribute<TAttribute>(out TAttribute? attribute) where TAttribute : Attribute => (attribute = GetAttribute<TAttribute>()) is not null;

        /// <inheritdoc />
        public override async ValueTask SetValue(object target, object? value)
        {
            if (PropertyChanged is not null)
            {
                var oldValue = await GetValue(target).ConfigureAwait(false);
                await base.SetValue(target, value).ConfigureAwait(false);
                var changedValue = await GetValue(target).ConfigureAwait(false);
                if (Equals(changedValue, oldValue))
                    return;
                NotifyChange(target, oldValue, value);
                return;
            }
            await base.SetValue(target, value).ConfigureAwait(false);
        }

        internal void NotifyChange(object target, object? oldValue, object? newValue)
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
            bool mergeOntoOwner,
            bool readOnly,
            Operators allowedConditionOperators,
            object? excelReducer,
            Type owner,
            Getter? getter,
            Setter? setter
        )
            : base(owner)
        {
            MetadataToken = metadataToken;
            Name = name;
            Type = type;
            ActualName = actualName;
            Order = order;
            ExcelReducer = excelReducer;
            Attributes = attributes;
            SkipConditions = skipConditions;
            Hidden = hidden;
            HiddenIfNull = hiddenIfNull;
            IsEnum = type.IsEnum || type.IsNullable(out var @base) && @base!.IsEnum;
            IsCollection = Type.ImplementsGenericInterface(typeof(ICollection<>));
            IsValueType = type.IsValueType;
            AllowedConditionOperators = allowedConditionOperators;
            IsNullable = !type.IsValueType || type.IsNullable(out _) || hidden;
            IsDateTime = type == typeof(DateTime) || type == typeof(DateTime?);
            Getter = getter;
            Setter = setter;
            MergeOntoOwner = mergeOntoOwner;
            ReadOnly = readOnly;
        }

        /// <summary>
        /// The regular constructor, called by the type cache when creating declared properties
        /// </summary>
        internal DeclaredProperty(PropertyInfo p, bool flagName = false) : base(p.DeclaringType)
        {
            MetadataToken = p.MetadataToken;
            Name = p.RESTableMemberName(flagName);
            Type = p.PropertyType;
            ActualName = p.Name;
            Attributes = p.GetCustomAttributes().ToList();
            var memberAttribute = GetAttribute<RESTableMemberAttribute>();
            MergeOntoOwner = memberAttribute?.MergeOntoOwner ?? false;
            ReadOnly = memberAttribute?.ReadOnly ?? false;
            Order = memberAttribute?.Order;
            ExcelReducer = null!;
            IsCollection = Type.ImplementsGenericInterface(typeof(ICollection<>));
            SkipConditions = memberAttribute?.SkipConditions == true || p.DeclaringType!.HasAttribute<RESTableViewAttribute>();
            Hidden = memberAttribute?.Hidden == true;
            HiddenIfNull = memberAttribute?.HiddenIfNull == true;
            AllowedConditionOperators = memberAttribute?.AllowedOperators ?? Operators.All;
            IsNullable = !p.PropertyType.IsValueType || p.PropertyType.IsNullable(out _) || Hidden;
            IsEnum = p.PropertyType.IsEnum || p.PropertyType.IsNullable(out var @base) && @base!.IsEnum;
            IsDateTime = p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?);
            IsValueType = p.PropertyType.IsValueType;
            if (memberAttribute?.ExcelReducerName is not null)
                ExcelReducer = MakeExcelReducer(memberAttribute.ExcelReducerName, p);
            Getter = p.CanRead ? p.MakeDynamicGetter() : null;
            Setter = p.CanWrite ? p.MakeDynamicSetter() : null;
            ReplaceOnUpdate = memberAttribute?.ReplaceOnUpdate == true;
        }

        private static object MakeExcelReducer(string methodName, PropertyInfo p)
        {
            try
            {
                var method = p.DeclaringType!.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception();
                return method.CreateDelegate(typeof(Func<,>).MakeGenericType(p.DeclaringType, typeof(string)));
            }
            catch
            {
                throw new Exception($"Invalid or unknown excel reduce function '{methodName}' for property '{p.Name}' in type '" +
                                    $"{p.DeclaringType!.GetRESTableTypeName()}'. Must be public parameterless instance method with " +
                                    "System.String as return type");
            }
        }

        internal async ValueTask<long> ByteCount(object target)
        {
            if (target is null) throw new NullReferenceException(nameof(target));
            return await GetValue(target).ConfigureAwait(false) switch
            {
                null => 0,
                string str => Encoding.UTF8.GetByteCount(str),
                byte[] binary => binary.Length,
                _ => Type.CountBytes()
            };
        }

        /// <inheritdoc />
        public override string ToString() => $"{Type.GetRESTableTypeName()}.{Name}";

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DeclaredProperty p && p.MetadataToken == MetadataToken;

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
            public bool Equals(DeclaredProperty? x, DeclaredProperty? y) => x?.Name == y?.Name;
            public int GetHashCode(DeclaredProperty obj) => obj.Name.GetHashCode();
        }

        private class _IdentityComparer : IEqualityComparer<DeclaredProperty>
        {
            public bool Equals(DeclaredProperty? x, DeclaredProperty? y) => x?.MetadataToken == y?.MetadataToken;
            public int GetHashCode(DeclaredProperty obj) => obj.MetadataToken;
        }
    }
}