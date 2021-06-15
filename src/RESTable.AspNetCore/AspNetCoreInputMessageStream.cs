using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreInputMessageStream : AspNetCoreMessageStream, IAsyncDisposable
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public AspNetCoreInputMessageStream
        (
            WebSocket webSocket,
            WebSocketReceiveResult initialResult,
            CancellationToken webSocketCancelledToken
        ) : base
        (
            webSocket: webSocket,
            messageType: initialResult.MessageType,
            webSocketCancelledToken: webSocketCancelledToken
        )
        {
            EndOfMessage = initialResult.EndOfMessage;
            ByteCount = initialResult.Count;
        }

        private async Task<int> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            EndOfMessage = result.EndOfMessage;
            ByteCount += result.Count;
            return result.Count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, WebSocketCancelledToken).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return 0;
            var arraySegment = new ArraySegment<byte>(buffer, offset, count);
            return await ReceiveAsync(arraySegment, cancellationToken).ConfigureAwait(false);
        }

        public override int ReadByte()
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return -1;
            var arraySegment = new ArraySegment<byte>(new byte[1], 0, 1);
            return ReceiveAsync(arraySegment, WebSocketCancelledToken).Result;
        }

#if !NETSTANDARD2_0
        private async Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            EndOfMessage = result.EndOfMessage;
            ByteCount += result.Count;
            return result.Count;
        }

        public override int Read(Span<byte> buffer)
        {
            var array = new byte[buffer.Length];
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return 0;
            var count = ReceiveAsync(array, WebSocketCancelledToken).Result;
            array.CopyTo(buffer);
            return count;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return 0;
            return await ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            var memory = new byte[4096];
            while (!EndOfMessage)
            {
                await ReadAsync(memory);
            }
        }
#else
        public override async ValueTask DisposeAsync()
        {
            var memory = new byte[4096];
            while (!EndOfMessage)
            {
                await ReadAsync(memory, 0, memory.Length);
            }
        }
#endif

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            DisposeAsync().AsTask().Wait();
        }
    }
}