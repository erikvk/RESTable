using System;
using RESTable.Requests;

namespace RESTable.WebSockets
{
    internal class WebSocketContext : RESTableContext
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        internal WebSocketContext(WebSocket webSocket, Client client) : base(client)
        {
            WebSocket = webSocket;
            Client.IsInWebSocket = true;
        }
    }
}