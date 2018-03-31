using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public class FeatureNotImplemented : Error
    {
        /// <inheritdoc />
        public FeatureNotImplemented(string message) : base(ErrorCodes.NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}