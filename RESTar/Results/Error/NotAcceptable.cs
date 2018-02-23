using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid or unsupported content type in the Accept header of a request.
    /// </summary>
    public class NotAcceptable : RESTarError
    {
        /// <inheritdoc />
        public NotAcceptable(string headerValue) : base(ErrorCodes.NotAcceptable,
            $"No supported media types in Accept header: {headerValue}")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }
}