using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.WebSockets
{
    internal class MultipleWebSocketsMessageStream : Stream
    {
        private WebSocketMessageType MessageType { get; }
        private CancellationToken CancellationToken { get; }
        private Stream[] MessageStreams { get; }

        public MultipleWebSocketsMessageStream(IEnumerable<Stream> messageStreams, bool asText, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MessageStreams = messageStreams.ToArray();
            MessageType = asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
            CancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeAsync().AsTask().Wait(CancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            foreach (var stream in MessageStreams)
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CancellationToken.ThrowIfCancellationRequested();
            return Task.WhenAll(MessageStreams.Select(s => s.WriteAsync(buffer, offset, count, cancellationToken)));
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            CancellationToken.ThrowIfCancellationRequested();
            foreach (var stream in MessageStreams)
            {
                await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CancellationToken.ThrowIfCancellationRequested();
            for (var index = 0; index < MessageStreams.Length; index++)
            {
                MessageStreams[index].Write(buffer);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var tasks = new Task[MessageStreams.Length];
            for (var index = 0; index < MessageStreams.Length; index++)
            {
                var i = index;
                tasks[i] = Task.Run(() => MessageStreams[i].Write(buffer, offset, count), CancellationToken);
            }
            Task.WhenAll(tasks).Wait(CancellationToken);
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