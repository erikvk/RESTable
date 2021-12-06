using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an unknown or unsupported input from a WebSocket
/// </summary>
internal class UnsupportedWebSocketInput : Error
{
    internal UnsupportedWebSocketInput(string info) : base(ErrorCodes.UnsupportedContent, info)
    {
        StatusCode = HttpStatusCode.UnsupportedMediaType;
        StatusDescription = "Unsupported media type";
    }
}