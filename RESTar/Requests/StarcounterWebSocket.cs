using Starcounter;
using WebSocket = Starcounter.WebSocket;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : WebSocket
    {
        private readonly Starcounter.Request ScRequest;
        private readonly string GroupName;
        private Starcounter.WebSocket WebSocket;

        protected override void Send(string text) => WebSocket.Send(text);
        protected override void Send(byte[] data, bool isText) => WebSocket.Send(data, isText);
        protected override bool IsConnected => !WebSocket.IsDead();
        protected override void DisconnectWebSocket() => WebSocket.Disconnect();
        protected override void SendUpgrade() => WebSocket = ScRequest.SendUpgrade(GroupName);

        internal StarcounterWebSocket(string groupName, Starcounter.Request scRequest, Client client)
            : base(DbHelper.Base64EncodeObjectNo(scRequest.GetWebSocketId()), client)
        {
            GroupName = groupName;
            ScRequest = scRequest;
        }
    }
}