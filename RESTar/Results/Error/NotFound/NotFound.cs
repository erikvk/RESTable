using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    internal abstract class NotFound : RESTarException
    {
        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}