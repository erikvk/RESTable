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
        public NotAcceptable(MimeType unsupported) : base(ErrorCodes.NotAcceptable,
            $"Unsupported accept format: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }
}