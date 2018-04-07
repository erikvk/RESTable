using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unknown or unsupported media type in request contents
    /// </summary>
    public class UnsupportedContent : Error
    {
        internal UnsupportedContent(Exception ie) : base(ErrorCodes.UnsupportedContent, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }

        /// <inheritdoc />
        public UnsupportedContent(string headerValue) : base(ErrorCodes.UnsupportedContent,
            $"An unsupported media type, '{headerValue}', was specified in the Content-Type header.")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}