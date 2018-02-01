using System;
using System.Net;
using RESTar.Internal;
using RESTar.Results.Error;

namespace RESTar.Results.Fail.BadRequest
{
    internal class BadRequest : RESTarError
    {
        internal BadRequest(ErrorCodes code, Exception ie) : base(code, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

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