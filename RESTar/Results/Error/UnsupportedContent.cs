using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// Thrown when a request had a non-supported Content-Type header
    /// </summary>
    internal class UnsupportedContent : RESTarException
    {
        internal UnsupportedContent(MimeType unsupported) : base(ErrorCodes.UnsupportedContent,
            $"Unsupported content type: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}