using System.Collections.Generic;
using RESTable.ProtocolProviders;

namespace RESTable.Requests
{
    /// <summary>
    /// Describes the components of a RESTable URI
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
        IReadOnlyCollection<IUriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        IReadOnlyCollection<IUriCondition> MetaConditions { get; }

        /// <summary>
        /// The macro, if any, belonging to these uri components
        /// </summary>
        IMacro Macro { get; }

        /// <summary>
        /// The protocol provider specified in the uri string
        /// </summary>
        IProtocolProvider ProtocolProvider { get; }
    }
}