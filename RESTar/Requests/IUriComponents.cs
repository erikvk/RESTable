using System.Collections.Generic;

namespace RESTar.Requests {
    /// <summary>
    /// Contains parameters for a RESTar URI
    /// </summary>
    public interface IUriComponents
    {
        /// <summary>
        /// Specifies the resource for the request
        /// </summary>
        string ResourceSpecifier { get; }

        /// <summary>
        /// Specifies the view for the request
        /// </summary>
        string ViewName { get; }

        /// <summary>
        /// Specifies the conditions for the request
        /// </summary>
        IEnumerable<IUriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        IEnumerable<IUriCondition> MetaConditions { get; }

        /// <summary>
        /// Generates a URI string from these components, according to some protocol.
        /// If null, the default protocol is used.
        /// </summary>
        string ToUriString(string protocolIdentifier = null);
    }
}