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
        private bool IgnoreCertificateErrors { get; }

        public AspNetCoreClientWebSocket(ClientWebSocket webSocket, Uri remoteUri, string webSocketId, RESTableContext context, bool ignoreCertificateErrors)
            : base(webSocketId, context)
        {
            ClientWebSocket = webSocket;
            WebSocket = webSocket;
            RemoteUri = remoteUri;
            IgnoreCertificateErrors = ignoreCertificateErrors;
        }

        protected override Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken)
        {
#if !NETSTANDARD2_0
            if (IgnoreCertificateErrors)
            {
                ClientWebSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }
#endif
            return ClientWebSocket.ConnectAsync(RemoteUri, cancellationToken);
        }
    }
}