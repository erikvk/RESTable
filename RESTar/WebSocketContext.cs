using System;

namespace RESTar {
    internal class WebSocketContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        internal WebSocketContext(Client client) : base(client, false) => Client.IsInWebSocket = true;
    }
}