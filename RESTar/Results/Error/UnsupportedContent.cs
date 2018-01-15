using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class UnsupportedContent : RESTarError
    {
        internal UnsupportedContent(Exception ie) : base(ErrorCodes.UnsupportedContent, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }

        internal UnsupportedContent(MimeType unsupported) : base(ErrorCodes.UnsupportedContent,
            $"Unsupported content type: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}