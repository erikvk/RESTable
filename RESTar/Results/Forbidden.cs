using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a forbidden operation
    /// search string.
    /// </summary>
    public abstract class Forbidden : Error
    {
        /// <inheritdoc />
        protected Forbidden(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal Forbidden(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }
}