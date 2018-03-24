using System;

namespace RESTar {
    internal class InternalContext : Context
    {
        protected override WebSocket GetWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        internal InternalContext(Client client = null, bool autoDisposeClient = true) : base(client ?? Client.Internal, autoDisposeClient) { }
    }
}