using System.Net;
using RESTar.Internal;
using RESTar.Results.Error;

namespace RESTar.Results.Fail.NotFound
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    internal abstract class NotFound : RESTarError
    {
        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}