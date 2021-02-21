using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreWebSocket : WebSockets.WebSocket
    {
        private HttpContext HttpContext { get; }
        private WebSocket WebSocket { get; set; }

        public AspNetCoreWebSocket(HttpContext httpContext, string webSocketId, RESTableContext context, Client client) : base(webSocketId, context, client)
        {
            HttpContext = httpContext;
        }

        protected override async Task Send(string text, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(buffer);
            await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task Send(ArraySegment<byte> data, bool asText, CancellationToken cancellationToken)
        {
            await WebSocket.SendAsync(data, asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<long> Send(Stream data, bool asText, CancellationToken token)
        {
            var buffer = new byte[4096];
            var messageType = asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
            long bytesSent = 0;
            while (data.CanRead)
            {
                var readToBuffer = await data.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                var wsBuffer = new ArraySegment<byte>(buffer, 0, readToBuffer);
                await WebSocket.SendAsync(wsBuffer, messageType, readToBuffer < buffer.Length, token).ConfigureAwait(false);
                if (readToBuffer == 0) break;
                bytesSent += readToBuffer;
            }
            return bytesSent;
        }

        public override Stream GetOutputStream(bool asText)
        {
            return new AspNetCoreWebSocketMessageStream(WebSocket, asText, CancellationTokenSource.Token);
        }

        protected override bool IsConnected => WebSocket.State == WebSocketState.Open;

        protected override async Task SendUpgrade()
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        }

        protected override async Task InitLifetimeTask(CancellationToken cancellationToken)
        {
            while (await ReceiveStreamAsync(cancellationToken).ConfigureAwait(false) is var (byteArray, isBinary) && !WebSocket.CloseStatus.HasValue)
            {
                try
                {
                    if (isBinary)
                    {
                        await HandleBinaryInput(byteArray).ConfigureAwait(false);
                    }
                    else
                    {
                        var str = Encoding.UTF8.GetString(byteArray);
                        await HandleTextInput(str).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    await Send(e.Message, cancellationToken).ConfigureAwait(false);
                    await DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task<(byte[] data, bool isText)> ReceiveStreamAsync(CancellationToken ct = default)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var ms = new SwappingStream();

            WebSocketReceiveResult result;
            do
            {
                ct.ThrowIfCancellationRequested();
                result = await WebSocket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
                if (buffer.Array != null)
                    await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count, ct).ConfigureAwait(false);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            return (
                data: await ms.GetBytesAsync().ConfigureAwait(false),
                isText: result.MessageType == WebSocketMessageType.Binary
            );
        }

        protected override async Task Close()
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }
    }
}