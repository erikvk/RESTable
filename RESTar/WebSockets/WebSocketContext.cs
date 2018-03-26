using System;

namespace RESTar.WebSockets
{
    internal class WebSocketContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        internal WebSocketContext(WebSocket webSocket, Client client) : base(client, false)
        {
            WebSocket = webSocket;
            Client.IsInWebSocket = true;
        }
    }
}