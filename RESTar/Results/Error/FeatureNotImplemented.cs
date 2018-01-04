using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class FeatureNotImplemented : RESTarError
    {
        internal FeatureNotImplemented(string message) : base(ErrorCodes.NotImplemented, message)
        {
            StatusCode = HttpStatusCode.NotImplemented;
            StatusDescription = "Not implemented";
        }
    }
}