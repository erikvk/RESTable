using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public abstract class BadRequest : Error
    {
        /// <inheritdoc />
        protected BadRequest(ErrorCodes code, Exception ie) : base(code, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

        /// <inheritdoc />
        protected BadRequest(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }

        /// <inheritdoc />
        protected BadRequest(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }
}