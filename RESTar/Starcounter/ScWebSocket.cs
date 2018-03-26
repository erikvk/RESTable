using Starcounter;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Starcounter
{
    internal class ScWebSocket : WebSocket
    {
        private readonly global::Starcounter.Request UpgradeRequest;
        private readonly string GroupName;
        private global::Starcounter.WebSocket WebSocket;

        protected override void Send(string text) => WebSocket.Send(text);
        protected override void Send(byte[] data, bool isText) => WebSocket.Send(data, isText);
        protected override bool IsConnected => !WebSocket.IsDead();
        protected override void DisconnectWebSocket() => WebSocket.Disconnect();
        protected override void SendUpgrade() => WebSocket = UpgradeRequest.SendUpgrade(GroupName);

        internal ScWebSocket(string groupName, global::Starcounter.Request upgradeRequest, Client client)
            : base(DbHelper.Base64EncodeObjectNo(upgradeRequest.GetWebSocketId()), client)
        {
            GroupName = groupName;
            UpgradeRequest = upgradeRequest;
        }
    }
}