using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Tests
{
    public class MockWebSocket : WebSocket
    {
        public MockWebSocket(string webSocketId, RESTableContext context, Client client) : base(webSocketId, context, client) { }

        protected override Task Send(string text, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task Send(ArraySegment<byte> data, bool asText, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task<long> Send(Stream data, bool asText, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task<Stream> GetOutgoingMessageStream(bool asText, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override bool IsConnected => throw new NotImplementedException();

        protected override Task SendUpgrade()
        {
            throw new NotImplementedException();
        }

        protected override Task InitMessageReceiveListener(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task Close()
        {
            throw new NotImplementedException();
        }
    }
}