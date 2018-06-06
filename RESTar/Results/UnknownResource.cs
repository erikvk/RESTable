using System;
using System.Text.RegularExpressions;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource by some search string
    /// </summary>
    internal class UnknownResource : NotFound
    {
        /// <inheritdoc />
        public UnknownResource(ErrorCodes code, string info, Exception ie) : base(code, info, ie) { }

        internal UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'. " +
            (Regex.IsMatch(searchString, @"[^\w\d\.]+") ? " Check request URI syntax." : "")) { }
    }
}