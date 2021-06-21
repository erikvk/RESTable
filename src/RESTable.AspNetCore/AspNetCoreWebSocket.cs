using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using static System.Net.WebSockets.WebSocketMessageType;

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
            await WebSocket.SendAsync(segment, Text, true, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task Send(ArraySegment<byte> data, bool asText, CancellationToken cancellationToken)
        {
            await WebSocket.SendAsync(data, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<long> Send(Stream data, bool asText, CancellationToken token)
        {
            var buffer = new byte[4096];
            var messageType = asText ? Text : Binary;
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
                messageType: asText ? Text : Binary,
                webSocketCancelledToken: cancellationToken
            );
            return Task.FromResult<Stream>(messageStream);
        }

        protected override bool IsConnected => WebSocket.State == WebSocketState.Open;

        protected abstract override Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken);

#if !NETSTANDARD2_0
        private readonly ArraySegment<byte> EmptyBuffer = ArraySegment<byte>.Empty;
#else
        private readonly ArraySegment<byte> EmptyBuffer = new(Array.Empty<byte>());
#endif

        protected override async Task InitMessageReceiveListener(CancellationToken cancellationToken)
        {
            while (!WebSocket.CloseStatus.HasValue)
            {
                try
                {
                    var nextMessageResult = await WebSocket.ReceiveAsync(EmptyBuffer, cancellationToken).ConfigureAwait(false);
                    var nextMessage = new AspNetCoreInputMessageStream(WebSocket, nextMessageResult, cancellationToken);
                    switch (nextMessage.MessageType)
                    {
                        case Binary:
                        {
                            // We await the handling of the entire binary message, and only await the next message 
                            // when the handler has been completed, since streams are not immutable.
                            await HandleBinaryInput(nextMessage, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        case Text:
                        {
                            // We read the entire message to a string, then fire and forget the handler for that
                            // message. This means we can handle multiple text messages in parallel, which is fine
                            // since strings are immutable.
                            await using (nextMessage)
                            {
                                using var reader = new StreamReader(nextMessage, Encoding.Default);
                                var stringMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                                HandleTextInput(stringMessage, cancellationToken);
                            }
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        protected override async Task Close(CancellationToken cancellationToken)
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        }
    }
}