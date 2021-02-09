using System;
using System.Net;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an unknown or unsupported media type in request contents
    /// </summary>
    public class UnsupportedContent : Error
    {
        internal UnsupportedContent(Exception ie) : base(ErrorCodes.UnsupportedContent, ie.Message, ie)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }

        /// <inheritdoc />
        internal UnsupportedContent(string headerValue, bool fromHeader = true) : base(ErrorCodes.UnsupportedContent,
            $"An unsupported media type, '{headerValue}', was specified {(fromHeader ? "in the Content-Type request header" : "for this operation")}.")
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(UnsupportedContent)};{Request?.Resource};{ErrorCode}";
    }
}