using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public abstract class Forbidden : RESTarError
    {
        protected Forbidden(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal Forbidden(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }
}