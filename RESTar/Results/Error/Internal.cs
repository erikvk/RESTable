using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal abstract class Internal : RESTarError
    {
        public Internal(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }
    }
}