using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreWebSocket : WebSockets.WebSocket
    {
        private HttpContext HttpContext { get; }
        private System.Net.WebSockets.WebSocket WebSocket { get; set; }

        public AspNetCoreWebSocket(HttpContext httpContext, string webSocketId, Client client) : base(webSocketId, client)
        {
            HttpContext = httpContext;
        }

        protected override async Task Send(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(buffer);
            await WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        protected override async Task Send(byte[] data, bool asText, int offset, int length)
        {
            var segment = new ArraySegment<byte>(data);
            await WebSocket.SendAsync(segment, asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        protected override bool IsConnected => !WebSocket.CloseStatus.HasValue;

        protected override async Task SendUpgrade()
        {
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        }

        protected override async Task InitLifetimeTask()
        {
            while (await ReceiveStreamAsync() is var (byteArray, isBinary) && !WebSocket.CloseStatus.HasValue)
            {
                try
                {
                    if (isBinary)
                    {
                        await WebSocketController.HandleBinaryInput(Id, byteArray);
                    }
                    else
                    {
                        var str = Encoding.UTF8.GetString(byteArray);
                        await WebSocketController.HandleTextInput(Id, str);
                    }
                }
                catch (Exception e)
                {
                    await Send(e.Message);
                    await DisposeAsync();
                }
            }
        }

        private async Task<(byte[] data, bool isText)> ReceiveStreamAsync(CancellationToken ct = default)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var ms = new MemoryStream();

            WebSocketReceiveResult result;
            do
            {
                ct.ThrowIfCancellationRequested();
                result = await WebSocket.ReceiveAsync(buffer, ct);
                if (buffer.Array != null)
                    await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count, ct);
            } while (!result.EndOfMessage);

            return (
                data: ms.ToArray(),
                isText: result.MessageType == WebSocketMessageType.Binary
            );
        }

        protected override async Task Close()
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }
}