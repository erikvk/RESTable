using System;
using static System.AttributeTargets;
using static RESTar.Operators;

namespace RESTar
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(Property | Field)]
    public class RESTarMemberAttribute : Attribute
    {
        /// <summary>
        /// Should this property be completely ignored by RESTar? Equivalent to using the .NET standard IgnoreDataMemberAttribute 
        /// </summary>
        public bool Ignored { get; }

        /// <summary>
        /// A new name for this property, used by RESTar. Equivalent to using the .NET standard DataMemberAttribute attribute.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The order at which this property appears when all properties are enumerated
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Should this property be hidden in output by default? It can still be added and queried against.
        /// To make RESTar completely ignore a property, use the Ignored property.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Should this property be hidden in output if the value is null? Only applies to JSON output.
        /// </summary>
        public bool HiddenIfNull { get; }

        /// <summary>
        /// Makes this property read only over the REST API, even if it has a public setter.
        /// </summary>
        public bool ReadOnly { get; }

        /// <summary>
        /// Should the serializer flatten this property using the ToString() method when writing to excel?
        /// </summary>
        public bool ExcelFlattenToString { get; }

        /// <summary>
        /// Sets the Skip property of all conditions matched against this property to true by default.
        /// </summary>
        public bool SkipConditions { get; }

        /// <summary>
        /// These operators will be allowed in conditions targeting this property.
        /// </summary>
        public Operators AllowedOperators { get; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new RESTar property configuration for a property
        /// </summary>
        /// <param name="ignore">Should this property be completely ignored by RESTar?</param>
        /// <param name="name">A new name for this property, used by RESTar. Equivalent to using the .NET standard DataMemberAttribute attribute</param>
        /// <param name="order">The order at which this property appears when all properties are enumerated</param>
        /// <param name="hide">Should this property be hidden in serialized output by default? It can still be added and queried against.</param>
        /// <param name="hideIfNull">Should this property be hidden in output if the value is null? Only applies to JSON output.</param>
        /// <param name="readOnly">Makes this property read-only over the REST API, even if it has a public setter.</param>
        /// <param name="excelFlattenToString">Should the serializer flatten this property using the ToString() method when writing to excel?</param>
        /// <param name="skipConditions">Sets the Skip property of all conditions matched against this property to true by default.</param>
        /// <param name="allowedOperators">These operators will be allowed in conditions targeting this property.</param>
        public RESTarMemberAttribute(bool ignore = false, string name = null, int order = 0, bool hide = false,
            bool hideIfNull = false, bool readOnly = false, bool excelFlattenToString = false, bool skipConditions = false,
            Operators allowedOperators = EQUALS | NOT_EQUALS | LESS_THAN | LESS_THAN_OR_EQUALS | GREATER_THAN | GREATER_THAN_OR_EQUALS)
        {
            Ignored = ignore;
            Name = name;
            Order = order;
            Hidden = hide;
            HiddenIfNull = hideIfNull;
            ReadOnly = readOnly;
            ExcelFlattenToString = excelFlattenToString;
            SkipConditions = skipConditions;
            AllowedOperators = allowedOperators;
        }
    }
}