using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : Error
    {
        /// <inheritdoc />
        public override string Metadata => $"{nameof(NotFound)};{RequestInternal.Resource};{ErrorCode}";

        /// <inheritdoc />
        protected NotFound(ErrorCodes code, string info, Exception ie) : base(code, info, ie) { }

        internal NotFound(ErrorCodes code, string info) : base(code, info)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}