using System;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class InternalContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        internal InternalContext(Client client = null, bool autoDisposeClient = true) : base(client ?? Client.Internal, autoDisposeClient) { }
    }
}