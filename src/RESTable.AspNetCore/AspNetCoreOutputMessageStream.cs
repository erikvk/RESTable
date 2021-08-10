using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore
{
    internal sealed class AspNetCoreOutputMessageStream : AspNetCoreMessageStream, IAsyncDisposable
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public AspNetCoreOutputMessageStream
        (
            WebSocket webSocket,
            WebSocketMessageType messageType,
            CancellationToken webSocketCancelledToken
        ) : base
        (
            webSocket: webSocket,
            messageType: messageType,
            webSocketCancelledToken: webSocketCancelledToken
        ) { }


        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, WebSocketCancelledToken).Wait(WebSocketCancelledToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            await WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(buffer, offset, count),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            ByteCount += count;
        }

        public override void WriteByte(byte value)
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(new[] { value }),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: WebSocketCancelledToken
            ).Wait(WebSocketCancelledToken);
            ByteCount += 1;
        }

#if NETSTANDARD2_0
        public override async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            await base.DisposeAsync();
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (IsDisposed) return;
            await WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(Array.Empty<byte>()),
                messageType: MessageType,
                endOfMessage: true,
                cancellationToken: WebSocketCancelledToken
            ).ConfigureAwait(false);
            IsDisposed = true;
        }

#else
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            await WebSocket.SendAsync
            (
                buffer: buffer,
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            ByteCount += buffer.Length;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            WebSocket.SendAsync
            (
                buffer: buffer.ToArray(),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: WebSocketCancelledToken
            ).Wait(WebSocketCancelledToken);
            ByteCount += buffer.Length;
        }

        public override async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (IsDisposed) return;
            await WebSocket.SendAsync
            (
                buffer: ReadOnlyMemory<byte>.Empty,
                messageType: MessageType,
                endOfMessage: true,
                cancellationToken: WebSocketCancelledToken
            ).ConfigureAwait(false);
            IsDisposed = true;
        }

#endif

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}