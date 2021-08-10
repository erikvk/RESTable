using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.AspNetCore
{
    internal sealed class AspNetCoreClientWebSocket : AspNetCoreWebSocket
    {
        private ClientWebSocket ClientWebSocket { get; }
        private Uri RemoteUri { get; }

        public AspNetCoreClientWebSocket(ClientWebSocket webSocket, Uri remoteUri, string webSocketId, RESTableContext context)
            : base(webSocketId, context)
        {
            ClientWebSocket = webSocket;
            WebSocket = webSocket;
            RemoteUri = remoteUri;
        }

        protected override Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken)
        {
            return ClientWebSocket.ConnectAsync(RemoteUri, cancellationToken);
        }
    }
}