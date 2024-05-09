namespace RESTable.WebSockets;

public enum WebSocketMessageStreamMode
{
    /// <summary>
    ///     Throw an exception if any of the underlying WebsScket connections are not open when writing to the stream
    /// </summary>
    Strict,

    /// <summary>
    ///     Write to the stream if the underlying WebSocket connection is open, otherwise ignore the write. Use this mode
    ///     if a response mechanism is used to verify that the message was received.
    /// </summary>
    Broadcast
}
