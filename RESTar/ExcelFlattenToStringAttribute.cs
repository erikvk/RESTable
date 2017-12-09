using System;

namespace RESTar {
    /// <inheritdoc />
    /// <summary>
    /// An attribute that can be used to decorate field and property declarations, and tell
    /// the serializer to flatten them using the ToString() method when writing to excel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Obsolete("Use RESTarMemberAttribute instead")]
    public class ExcelFlattenToStringAttribute : Attribute { }
}