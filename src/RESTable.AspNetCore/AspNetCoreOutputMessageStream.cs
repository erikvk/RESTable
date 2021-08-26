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
        
        private SemaphoreSlim WriteSemaphore { get; }

        private bool SemaphoreOpen { get; set; }

        public override long Position
        {
            get => ByteCount;
            set => throw new NotSupportedException();
        }

        public AspNetCoreOutputMessageStream(WebSocket webSocket, WebSocketMessageType messageType, SemaphoreSlim writeSemaphore, CancellationToken webSocketCancelledToken)
            : base(webSocket, messageType, webSocketCancelledToken)
        {
            WriteSemaphore = writeSemaphore;
        }

#if NETSTANDARD2_0
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (IsDisposed)
                throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
            if (!SemaphoreOpen)
            {
                var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken).Token;
                await WriteSemaphore.WaitAsync(combinedToken).ConfigureAwait(false);
                SemaphoreOpen = true;
            }
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

        public override async ValueTask DisposeAsync()
        {
            if (!SemaphoreOpen || IsDisposed) return;
            try
            {
                await WebSocket.SendAsync
                (
                    buffer: new ArraySegment<byte>(Array.Empty<byte>()),
                    messageType: MessageType,
                    endOfMessage: true,
                    cancellationToken: WebSocketCancelledToken
                ).ConfigureAwait(false);
                IsDisposed = true;
            }
            finally
            {
                WriteSemaphore.Release();
                SemaphoreOpen = false;
            }
        }
#else
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            if (IsDisposed)
                throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
            if (!SemaphoreOpen)
            {
                var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken).Token;
                await WriteSemaphore.WaitAsync(combinedToken).ConfigureAwait(false);
                SemaphoreOpen = true;
            }
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

        public override async ValueTask DisposeAsync()
        {
            if (!SemaphoreOpen || IsDisposed) return;
            try
            {
                await WebSocket.SendAsync
                (
                    buffer: ReadOnlyMemory<byte>.Empty,
                    messageType: MessageType,
                    endOfMessage: true,
                    cancellationToken: WebSocketCancelledToken
                ).ConfigureAwait(false);
                IsDisposed = true;
            }
            finally
            {
                WriteSemaphore.Release();
                SemaphoreOpen = false;
            }
        }
#endif

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}