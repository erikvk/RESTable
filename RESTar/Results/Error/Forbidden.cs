using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client does something that is forbidden
    /// </summary>
    public class Forbidden : RESTarException
    {
        internal Forbidden(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }
}