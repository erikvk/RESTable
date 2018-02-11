using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : RESTarError
    {
        /// <inheritdoc />
        protected NotFound(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}