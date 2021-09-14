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
        private const int MaxNumberOfConcurrentWriters = 1;

        /// <summary>
        /// Ensures that at most one thread sends a message frame over this websocket at any one time, to
        /// avoid frame fragmentation.
        /// </summary>
        private SemaphoreSlim SendMessageSemaphore { get; }

        protected WebSocket WebSocket { get; set; }
        private ArrayPool<byte> ArrayPool { get; }
        private int WebSocketBufferSize { get; }

        protected AspNetCoreWebSocket(string webSocketId, RESTableContext context) : base(webSocketId, context)
        {
            WebSocketBufferSize = context.Configuration.WebSocketBufferSize;
            ArrayPool = ArrayPool<byte>.Create(WebSocketBufferSize, 32);
            WebSocket = null!;
            SendMessageSemaphore = new SemaphoreSlim(MaxNumberOfConcurrentWriters, MaxNumberOfConcurrentWriters);
        }

        protected override async Task SendBuffered(string data, bool asText, CancellationToken cancellationToken)
        {
            await SendMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var buffer = Encoding.UTF8.GetBytes(data);
#if NETSTANDARD2_0
                var byteData = new ArraySegment<byte>(buffer);
#else
                var byteData = buffer.AsMemory();
#endif
                await WebSocket.SendAsync(byteData, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SendMessageSemaphore.Release();
            }
        }

        protected override async Task SendBuffered(Memory<byte> data, bool asText, CancellationToken cancellationToken)
        {
            await SendMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
#if NETSTANDARD2_0
                var segment = new ArraySegment<byte>(data.ToArray());
                await WebSocket.SendAsync(segment, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
#else
                await WebSocket.SendAsync(data, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
#endif
            }
            finally
            {
                SendMessageSemaphore.Release();
            }
        }

        protected override Stream GetOutgoingMessageStream(bool asText, CancellationToken cancellationToken)
        {
            return new AspNetCoreOutputMessageStream
            (
                webSocket: WebSocket,
                messageType: asText ? Text : Binary,
                SendMessageSemaphore,
                webSocketCancelledToken: cancellationToken
            );
        }

        protected override bool IsConnected => WebSocket.State == WebSocketState.Open;

        protected abstract override Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken);

#if NETSTANDARD2_0
        private static readonly ArraySegment<byte> EmptyBuffer = new(Array.Empty<byte>());
#endif

        private static async ValueTask<(WebSocketMessageType messageType, bool endOfMessage, int byteCount)> GetInitial
        (
            WebSocket webSocket,
            CancellationToken cancellationToken
        )
        {
#if NETSTANDARD2_0
            var initial = await webSocket.ReceiveAsync(EmptyBuffer, cancellationToken).ConfigureAwait(false);
            return (initial.MessageType, initial.EndOfMessage, initial.Count);
#else
            var initial = await webSocket.ReceiveAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
            return (initial.MessageType, initial.EndOfMessage, initial.Count);
#endif
        }

        protected override async Task InitMessageReceiveListener(CancellationToken cancellationToken)
        {
            try
            {
                try
                {
                    while (!WebSocket.CloseStatus.HasValue)
                    {
                        var (messageType, endOfMessage, byteCount) = await GetInitial(WebSocket, cancellationToken).ConfigureAwait(false);
                        switch (messageType)
                        {
                            case Binary:
                            {
                                var nextMessage = new AspNetCoreInputMessageStream(WebSocket, messageType, endOfMessage, byteCount, ArrayPool, WebSocketBufferSize,
                                    cancellationToken);
                                await using (nextMessage.ConfigureAwait(false))
                                {
                                    await HandleBinaryInput(nextMessage, cancellationToken).ConfigureAwait(false);
                                }
                                break;
                            }
                            case Text:
                            {
                                var nextMessage = new AspNetCoreInputMessageStream(WebSocket, messageType, endOfMessage, byteCount, ArrayPool, WebSocketBufferSize,
                                    cancellationToken);
                                Task handleTask;
                                await using (nextMessage.ConfigureAwait(false))
                                {
                                    using (var reader = new StreamReader(nextMessage, Encoding.Default, true, 4096, leaveOpen: true))
                                    {
                                        var stringMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                                        handleTask = HandleTextInput(stringMessage, cancellationToken);
                                    }
                                }
                                await handleTask.ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                }
                catch (WebSocketException)
                {
                    // Client closed without completing close handshake
                }
                finally
                {
                    // WebSocket is closed
                    Cancel();
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
        }

        protected override async Task Close(CancellationToken cancellationToken)
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
        }
    }
}