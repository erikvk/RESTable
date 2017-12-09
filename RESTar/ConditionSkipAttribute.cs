using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Automatically sets the Skip property of conditions matched against this property to true
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Obsolete("Use RESTarMemberAttribute instead")]
    public class ConditionSkipAttribute : Attribute { }
}