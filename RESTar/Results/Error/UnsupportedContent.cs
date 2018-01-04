using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
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