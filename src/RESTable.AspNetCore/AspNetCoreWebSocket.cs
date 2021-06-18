using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.AspNetCore
{
    internal abstract class AspNetCoreWebSocket : WebSockets.WebSocket
    {
        protected WebSocket WebSocket { get; set; }

        public AspNetCoreWebSocket(string webSocketId, RESTableContext context) : base(webSocketId, context)
        {
            WebSocket = null!;
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
            bool lastFrame;
            do
            {
                var readToBuffer = await data.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                if (readToBuffer == 0) return bytesSent;
                var wsBuffer = new ArraySegment<byte>(buffer, 0, readToBuffer);
                lastFrame = readToBuffer < buffer.Length;
                await WebSocket.SendAsync(wsBuffer, messageType, lastFrame, token).ConfigureAwait(false);
                bytesSent += readToBuffer;
            } while (!lastFrame);
            return bytesSent;
        }

        protected override Task<Stream> GetOutgoingMessageStream(bool asText, CancellationToken cancellationToken)
        {
            var messageStream = new AspNetCoreOutputMessageStream
            (
                webSocket: WebSocket,
                messageType: asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                webSocketCancelledToken: cancellationToken
            );
            return Task.FromResult<Stream>(messageStream);
        }

        protected override bool IsConnected => WebSocket.State == WebSocketState.Open;

        protected abstract override Task ConnectUnderlyingWebSocket();

        protected override async Task InitMessageReceiveListener(CancellationToken cancellationToken)
        {
            while (await AwaitNextMessage(cancellationToken).ConfigureAwait(false) is var nextMessage && !WebSocket.CloseStatus.HasValue)
            {
                switch (nextMessage.MessageType)
                {
                    case WebSocketMessageType.Binary:
                    {
                        // We await the handling of the entire binary message, and only await the next message 
                        // when the handler has been completed, since streams are not immutable.
                        await HandleBinaryInput(nextMessage).ConfigureAwait(false);
                        break;
                    }
                    case WebSocketMessageType.Text:
                    {
                        // We read the entire message to a string, then fire and forget the handler for that
                        // message. This means we can handle multiple text messages in parallel, which is fine
                        // since strings are immutable.
                        await using (nextMessage)
                        {
                            using var reader = new StreamReader(nextMessage, Encoding.Default);
                            var stringMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                            HandleTextInput(stringMessage);
                        }
                        break;
                    }
                }
            }
        }

        private async Task<AspNetCoreMessageStream> AwaitNextMessage(CancellationToken cancellationToken)
        {
            var emptyBuffer = new ArraySegment<byte>(Array.Empty<byte>());
            var nextMessageResult = await WebSocket.ReceiveAsync(emptyBuffer, cancellationToken).ConfigureAwait(false);
            return new AspNetCoreInputMessageStream(WebSocket, nextMessageResult, cancellationToken);
        }

        protected override async Task Close()
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }
    }
}