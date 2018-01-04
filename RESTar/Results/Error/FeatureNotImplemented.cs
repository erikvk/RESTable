using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class FeatureNotImplemented : RESTarException
    {
        internal FeatureNotImplemented(string message) : base(ErrorCodes.NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}