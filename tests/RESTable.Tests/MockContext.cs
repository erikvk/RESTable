using System;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Tests
{
    public class MockContext : RESTableContext
    {
        public MockContext(Client client, IServiceProvider services = null) : base(client, services) { }

        protected override bool IsWebSocketUpgrade => false;

        protected override WebSocket CreateWebSocket()
        {
            throw new NotImplementedException();
        }
    }
}