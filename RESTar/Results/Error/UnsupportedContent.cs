using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unknown or unsupported media type in request contents
    /// </summary>
    public class UnsupportedContent : RESTarError
    {
        internal UnsupportedContent(Exception ie) : base(ErrorCodes.UnsupportedContent, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }

        /// <inheritdoc />
        public UnsupportedContent(MimeType unsupported) : base(ErrorCodes.UnsupportedContent,
            $"Unsupported content type: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}