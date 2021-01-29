using System;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Internal
{
    internal class InternalContext : RESTableContext
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        internal InternalContext(Client client = null) : base(client ?? Client.Internal) { }
    }
}