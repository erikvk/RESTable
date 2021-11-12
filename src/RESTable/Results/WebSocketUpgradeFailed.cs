using RESTable.WebSockets;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : ResultWrapper
    {
        public WebSocket WebSocket { get; }
        public Error Error { get; }

        internal WebSocketUpgradeFailed(Error error, WebSocket webSocket) : base(error)
        {
            WebSocket = webSocket;
            Error = error;
        }
    }
}