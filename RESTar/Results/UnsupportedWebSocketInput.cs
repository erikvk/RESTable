using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unknown or unsupported input from a WebSocket
    /// </summary>
    public class UnsupportedWebSocketInput : Error
    {
        /// <inheritdoc />
        public override string Metadata => $"{nameof(UnsupportedWebSocketInput)};{RequestInternal.Resource};{ErrorCode}";

        internal UnsupportedWebSocketInput(string info) : base(ErrorCodes.UnsupportedContent, info)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}