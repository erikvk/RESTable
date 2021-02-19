using System;
using System.Net;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Client
{
    internal class RemoteContext : RESTableContext
    {
        internal string ServiceRoot { get; }
        internal string ApiKey { get; }
        internal bool HasApiKey { get; }
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        public override IRequest CreateRequest(Method method = Method.GET, string uri = "/", object body = null, Headers headers = null)
        {
            return new RemoteRequest(this, method, uri, body, headers);
        }

        public override IRequest<T> CreateRequest<T>(Method method = Method.GET, string protocolId = "restable", string viewName = null)
        {
            throw new InvalidOperationException("Cannot create generic requests in remote contexts");
        }

        private static readonly Requests.Client RemoteClient = new(
            origin: (OriginType) (-1),
            host: "localhost",
            clientIp: new IPAddress(new byte[] {127, 0, 0, 1}),
            proxyIp: null,
            userAgent: null,
            https: false,
            cookies: new Cookies()
        );

        public RemoteContext(string serviceRoot, string apiKey = null) : base(RemoteClient)
        {
            ServiceRoot = serviceRoot.TrimEnd('/');
            ApiKey = apiKey;
            HasApiKey = ApiKey != null;
        }
    }
}