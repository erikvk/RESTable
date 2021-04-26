﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreMessageStream : Stream, IAsyncDisposable
    {
        private WebSocket WebSocket { get; }
        private WebSocketMessageType MessageType { get; }
        private CancellationToken CancellationToken { get; }
        private bool IsDisposed { get; set; }

        public AspNetCoreMessageStream(WebSocket webSocket, bool asText, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CancellationToken = cancellationToken;
            WebSocket = webSocket;
            MessageType = asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeAsync().AsTask().Wait(CancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CancellationToken.ThrowIfCancellationRequested();
            await WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(buffer, offset, count),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

#if NETSTANDARD2_1
        public override ValueTask DisposeAsync() => DisposeImpl();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            CancellationToken.ThrowIfCancellationRequested();
            await WebSocket.SendAsync
            (
                buffer: buffer,
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CancellationToken.ThrowIfCancellationRequested();
            WebSocket.SendAsync
            (
                buffer: buffer.ToArray(),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: CancellationToken
            ).Wait(CancellationToken);
        }
#else
        public ValueTask DisposeAsync() => DisposeImpl();
#endif

        private async ValueTask DisposeImpl()
        {
            if (IsDisposed) return;
            await WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(Array.Empty<byte>()),
                messageType: MessageType,
                endOfMessage: true,
                cancellationToken: CancellationToken
            ).ConfigureAwait(false);
            IsDisposed = true;
        }

        public override void WriteByte(byte value)
        {
            CancellationToken.ThrowIfCancellationRequested();
            WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(new[] {value}),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: CancellationToken
            ).Wait(CancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CancellationToken.ThrowIfCancellationRequested();
            WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(buffer, offset, count),
                messageType: MessageType,
                endOfMessage: false,
                cancellationToken: CancellationToken
            ).Wait(CancellationToken);
        }

        public override void Flush() { }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        #region Unsupported

        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
        public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
        public override void SetLength(long value) => throw new InvalidOperationException();
        public override long Length => throw new InvalidOperationException();

        public override long Position
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        #endregion
    }
}