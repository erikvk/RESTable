using Starcounter;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Sc
{
    internal sealed class ScContext : Context
    {
        private const string WsGroupName = "restar_ws";

        internal Request Request { get; }
        protected override bool IsWebSocketUpgrade { get; }

        protected override WebSocket CreateWebSocket()
        {
            return new ScWebSocket(ScNetworkProvider.WsGroupName, Request, Client);
        }

        public ScContext(Client client, Request request) : base(client)
        {
            Request = request;
            IsWebSocketUpgrade = request.WebSocketUpgrade;
        }
    }
}