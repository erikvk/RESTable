namespace RESTar.Starcounter
{
    internal sealed class ScContext : Context
    {
        private const string WsGroupName = "restar_ws";

        internal global::Starcounter.Request Request { get; }
        protected override bool IsWebSocketUpgrade { get; }
        protected override WebSocket CreateWebSocket() => new ScWebSocket(StarcounterNetworkProvider.WsGroupName, Request, Client);

        public ScContext(Client client, global::Starcounter.Request request) : base(client)
        {
            Request = request;
            IsWebSocketUpgrade = request.WebSocketUpgrade;
        }

        internal new ScWebSocket WebSocket
        {
            get => (ScWebSocket) GetWebSocket();
            set => SetWebSocket(value);
        }
    }
}