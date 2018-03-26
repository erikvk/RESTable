using System;
using System.Collections.Generic;

namespace RESTar.Queries
{
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
        /// A function that generates a string representation of the URI components,
        /// according to some pre-defined protocol (for example, the protocol of a 
        /// request).
        /// </summary>
        Func<IUriComponents, string> StringMaker { get; }
    }
}