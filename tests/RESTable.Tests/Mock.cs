using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RESTable.NetworkProviders;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Tests
{
    public class MockContext : RESTableContext
    {
        public MockContext(Client client) : base(client) { }

        protected override bool IsWebSocketUpgrade => throw new System.NotImplementedException();

        protected override WebSocket CreateWebSocket()
        {
            throw new System.NotImplementedException();
        }
    }

    public class MockWebSocket : WebSocket
    {
        public MockWebSocket(string webSocketId, Client client) : base(webSocketId, client) { }

        protected override Task Send(string text, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        protected override Task Send(byte[] data, bool asText, int offset, int length, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        protected override bool IsConnected => throw new System.NotImplementedException();

        protected override Task SendUpgrade()
        {
            throw new System.NotImplementedException();
        }

        protected override Task InitLifetimeTask(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        protected override Task Close()
        {
            throw new System.NotImplementedException();
        }
    }

    public class MockNetworkProvider : INetworkProvider
    {
        private static Client GetMockClient => Client.External
        (
            clientIp: IPAddress.Parse("151.10.10.5"),
            proxyIp: null,
            userAgent: "Some User-Agent!",
            host: "the host header",
            https: true,
            cookies: new Cookies()
        );

        public static Task<HttpStatusCode> MakeRequest(Method method, string uri, Stream body, Headers headers)
        {
            return default;
        }

        public void AddRoutes(Method[] methods, string rootUri, ushort port)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveRoutes(Method[] methods, string uri, ushort port)
        {
            throw new System.NotImplementedException();
        }
    }
}