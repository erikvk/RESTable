using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unknown or unsupported input from a WebSocket
    /// </summary>
    public class UnsupportedWebSocketInput : RESTarError
    {
        internal UnsupportedWebSocketInput(string message) : base(ErrorCodes.UnsupportedContent, message)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}