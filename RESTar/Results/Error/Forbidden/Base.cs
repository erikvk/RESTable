using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client does something that is forbidden
    /// </summary>
    public abstract class Base : RESTarException
    {
        internal Base(ErrorCodes code, string message) : base(code, message)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }
    }
}