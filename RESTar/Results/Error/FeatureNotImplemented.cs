using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// Thrown when RESTar encounters an unknown or not implemented feature
    /// </summary>
    internal class FeatureNotImplemented : RESTarException
    {
        internal FeatureNotImplemented(string message) : base(ErrorCodes.NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}