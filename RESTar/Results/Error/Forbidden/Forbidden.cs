using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal abstract class Forbidden : RESTarException
    {
        internal Forbidden(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }
}