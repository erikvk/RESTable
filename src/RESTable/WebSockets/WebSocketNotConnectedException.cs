namespace RESTable.WebSockets;

/// <inheritdoc />
/// <summary>
///     Throw when RESTable encounters an attempt to interact with a closed WebSocket connection
/// </summary>
public class WebSocketNotConnectedException : RESTableException
{
    internal WebSocketNotConnectedException() : base(ErrorCodes.WebSocketNotConnected,
        "An attempt was made to interact with a closed WebSocket connection") { }
}
