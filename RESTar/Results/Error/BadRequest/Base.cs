using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class Base : RESTarException
    {
        internal Base(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

        internal Base(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }
}