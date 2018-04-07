using System;
using Starcounter;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Sc
{
    internal class ScWebSocket : WebSocket
    {
        private readonly Request UpgradeRequest;
        private readonly string GroupName;
        private Starcounter.WebSocket WebSocket;

        protected override void Send(string text) => WebSocket.Send(text);

        protected override void Send(byte[] data, bool isText, int offset, int length)
        {
            if (offset == 0)
                WebSocket.Send(data, length, isText);
            else
            {
                var buffer = new byte[length];
                Array.Copy(data, offset, buffer, 0, length);
                WebSocket.Send(buffer, length, isText);
            }
        }

        protected override bool IsConnected => WebSocket?.IsDead() == false;
        protected override void DisconnectWebSocket(string message = null) => WebSocket.Disconnect(message);
        protected override void SendUpgrade() => WebSocket = UpgradeRequest.SendUpgrade(GroupName);

        internal ScWebSocket(string groupName, Request upgradeRequest, Client client)
            : base(DbHelper.Base64EncodeObjectNo(upgradeRequest.GetWebSocketId()), client)
        {
            GroupName = groupName;
            UpgradeRequest = upgradeRequest;
        }
    }
}