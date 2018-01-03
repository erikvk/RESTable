using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// Exceptions that should be treated as bad requests
    /// </summary>
    public abstract class NotFound : RESTarException
    {
        internal NotFound(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.NotFound;
            StatusDescription = "Not found";
        }
    }
}