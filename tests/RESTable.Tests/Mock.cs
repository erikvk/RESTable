using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;
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

    public class MockWebSocket : WebSocket
    {
        public MockWebSocket(string webSocketId, RESTableContext context, Client client) : base(webSocketId, context, client) { }

        protected override Task Send(string text, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task Send(byte[] data, bool asText, int offset, int length, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override bool IsConnected => throw new NotImplementedException();

        protected override Task SendUpgrade()
        {
            throw new NotImplementedException();
        }

        protected override Task InitLifetimeTask(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task Close()
        {
            throw new NotImplementedException();
        }
    }

    public class RequestMaker
    {
        private Client MockClient => Client.External
        (
            clientIp: IPAddress.Parse("151.10.10.5"),
            proxyIp: null,
            userAgent: "Some User-Agent!",
            host: "the host header",
            https: true,
            cookies: new Cookies()
        );

        public async Task<HttpStatusCode> MakeRequest(Method method, string uri, object body, Headers headers)
        {
            var client = MockClient;
            var context = new MockContext(client);
            await using var request = context.CreateRequest(method, uri, body, headers);
            var result = await request.Evaluate();
            await using var serialized = await result.Serialize();
            return serialized.Result.StatusCode;
        }
    }
}