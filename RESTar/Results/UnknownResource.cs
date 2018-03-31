using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource by some search string
    /// </summary>
    public class UnknownResource : NotFound
    {
        /// <inheritdoc />
        public UnknownResource(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'.") { }
    }
}