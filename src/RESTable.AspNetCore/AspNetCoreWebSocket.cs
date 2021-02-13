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

        public AspNetCoreWebSocket(HttpContext httpContext, string webSocketId, Client client) : base(webSocketId, client)
        {
            HttpContext = httpContext;
        }

        protected override async Task Send(string text, CancellationToken cancellationToken)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(buffer);
            await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        protected override async Task Send(byte[] data, bool asText, int offset, int length, CancellationToken cancellationToken)
        {
            var segment = new ArraySegment<byte>(data);
            await WebSocket.SendAsync(segment, asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true, cancellationToken);
        }

        protected override bool IsConnected => WebSocket.State == WebSocketState.Open;

        protected override async Task SendUpgrade()
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        }

        protected override async Task InitLifetimeTask(CancellationToken cancellationToken)
        {
            while (await ReceiveStreamAsync(cancellationToken) is var (byteArray, isBinary) && !WebSocket.CloseStatus.HasValue)
            {
                try
                {
                    if (isBinary)
                    {
                        await HandleBinaryInput(byteArray);
                    }
                    else
                    {
                        var str = Encoding.UTF8.GetString(byteArray);
                        await HandleTextInput(str);
                    }
                }
                catch (Exception e)
                {
                    await Send(e.Message, cancellationToken);
                    await DisposeAsync();
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
                result = await WebSocket.ReceiveAsync(buffer, ct);
                if (buffer.Array != null)
                    await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count, ct);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);
            
            return (
                data: await ms.GetBytesAsync(),
                isText: result.MessageType == WebSocketMessageType.Binary
            );
        }

        protected override async Task Close()
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }
}