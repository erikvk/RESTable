using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public abstract class BadRequest : Error
    {
        /// <inheritdoc />
        public override string Metadata => $"{nameof(BadRequest)};{RequestInternal.Resource};{ErrorCode}";

        /// <inheritdoc />
        protected BadRequest(ErrorCodes code, string info, Exception ie = null) : base(code, info, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }
}