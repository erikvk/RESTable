using System;
using RESTar.Requests;
using Starcounter;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Internal.Sc
{
    internal class ScWebSocket : WebSocket
    {
        private readonly Request UpgradeRequest;
        private readonly string GroupName;
        private Starcounter.WebSocket WebSocket;

        protected override void Send(string text) => Scheduling.RunTask(() => WebSocket.Send(text)).Wait();

        protected override void Send(byte[] data, bool isText, int offset, int length)
        {
            if (offset == 0)
                Scheduling.RunTask(() => WebSocket.Send(data, length, isText)).Wait();
            else
            {
                Scheduling.RunTask(() =>
                {
                    var buffer = new byte[length];
                    Array.Copy(data, offset, buffer, 0, length);
                    WebSocket.Send(buffer, length, isText);
                }).Wait();
            }
        }

        protected override bool IsConnected => WebSocket?.IsDead() == false;
        protected override void DisconnectWebSocket(string message = null) => Scheduling.RunTask(() => WebSocket.Disconnect(message)).Wait();
        protected override void SendUpgrade() => Scheduling.RunTask(() => WebSocket = UpgradeRequest.SendUpgrade(GroupName)).Wait();

        internal ScWebSocket(string groupName, Request upgradeRequest, Client client)
            : base(DbHelper.Base64EncodeObjectNo(upgradeRequest.GetWebSocketId()), client)
        {
            GroupName = groupName;
            UpgradeRequest = upgradeRequest;
        }
    }
}