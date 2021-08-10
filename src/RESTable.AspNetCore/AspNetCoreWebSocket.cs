using System;
using System.Buffers;
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
        protected ArrayPool<byte> ArrayPool { get; }

        internal const int WebSocketBufferSize = 4096;

        public AspNetCoreWebSocket(string webSocketId, RESTableContext context) : base(webSocketId, context)
        {
            ArrayPool = ArrayPool<byte>.Create(WebSocketBufferSize, 32);
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
            var buffer = ArrayPool.Rent(WebSocketBufferSize);
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
                    var nextMessage = new AspNetCoreInputMessageStream(WebSocket, nextMessageResult, ArrayPool, cancellationToken);
                    await using var nextMessageDisposable = nextMessage.ConfigureAwait(false);
                    switch (nextMessage.MessageType)
                    {
                        case Binary:
                        {
                            await HandleBinaryInput(nextMessage, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        case Text:
                        {
                            string stringMessage;
                            await using (nextMessageDisposable)
                            using (var reader = new StreamReader(nextMessage, Encoding.Default, true, 4096, leaveOpen: true))
                            {
                                stringMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                            }
                            await HandleTextInput(stringMessage, cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        protected override async Task Close(CancellationToken cancellationToken)
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
        }
    }
}