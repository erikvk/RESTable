using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : RESTarError
    {
        protected NotFound(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}