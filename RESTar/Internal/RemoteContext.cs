using System;
using RESTar.Requests;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class RemoteContext : Context
    {
        internal string ServiceRoot { get; }
        internal string ApiKey { get; }
        internal bool HasApiKey { get; }
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;

        public override IRequest CreateRequest(Method method, string uri, byte[] body = null, Headers headers = null)
        {
            var remoteRequest = new RemoteRequest(this, method, uri, body, headers);
            return remoteRequest;
        }

        public override IRequest<T> CreateRequest<T>(Method method, string protocolId = null, string viewName = null) =>
            throw new NotImplementedException("Cannot create generic requests for remote resources");

        internal RemoteContext(string serviceRoot, string apiKey = null) : base(Client.Remote)
        {
            if (serviceRoot.EndsWith("/"))
                ServiceRoot = serviceRoot.TrimEnd('/');
            ServiceRoot = serviceRoot;


            ApiKey = apiKey;
            HasApiKey = ApiKey != null;
        }
    }
}