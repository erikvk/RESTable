using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    public class FeatureNotImplemented : RESTarError
    {
        /// <inheritdoc />
        public FeatureNotImplemented(string message) : base(ErrorCodes.NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}