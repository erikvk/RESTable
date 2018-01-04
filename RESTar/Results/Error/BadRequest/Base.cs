using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    internal abstract class BadRequest : RESTarException
    {
        internal BadRequest(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

        internal BadRequest(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }
}