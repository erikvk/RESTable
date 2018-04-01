using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public abstract class BadRequest : RequestError
    {
        /// <inheritdoc />
        protected BadRequest(IRequest request, ErrorCodes code, string info, Exception ie = null) : base(request, code, info, ie)
        {
            StatusCode = HttpStatusCode.BadRequest;
            StatusDescription = "Bad request";
        }
    }
}