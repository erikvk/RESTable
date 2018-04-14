using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public class FeatureNotImplemented : Error
    {
        /// <inheritdoc />
        public override string Metadata => $"{nameof(FeatureNotImplemented)};{RequestInternal.Resource};{ErrorCode}";
        /// <inheritdoc />
        public FeatureNotImplemented(string info) : base(ErrorCodes.NotImplemented, info)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}