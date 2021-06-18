using System;
using RESTable.Requests;

namespace RESTable.WebSockets
{
    internal class WebSocketContext : RESTableContext
    {
        protected override WebSocket CreateServerWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade => false;

        internal WebSocketContext(WebSocket webSocket, Client client, IServiceProvider services) : base(client, services)
        {
            WebSocket = webSocket;
            Client.IsInWebSocket = true;
        }
    }
}