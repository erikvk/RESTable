using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Makes a resource property with a public setter read only over the REST API
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Obsolete("Use RESTarMemberAttribute instead")]
    public class ReadOnlyAttribute : Attribute { }
}