using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore
{
    internal sealed class AspNetCoreInputMessageStream : AspNetCoreMessageStream, IAsyncDisposable
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        private ArrayPool<byte> ArrayPool { get; }

        public AspNetCoreInputMessageStream
        (
            WebSocket webSocket,
            WebSocketReceiveResult initialResult,
            ArrayPool<byte> arrayPool,
            CancellationToken webSocketCancelledToken
        ) : base
        (
            webSocket: webSocket,
            messageType: initialResult.MessageType,
            webSocketCancelledToken: webSocketCancelledToken
        )
        {
            ArrayPool = arrayPool;
            EndOfMessage = initialResult.EndOfMessage;
            ByteCount = initialResult.Count;
        }


        private async ValueTask<int> ArraySegmentReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
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
            return await ArraySegmentReceiveAsync(arraySegment, cancellationToken).ConfigureAwait(false);
        }

        public override int ReadByte()
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return -1;
            var array = ArrayPool.Rent(1);
            var arraySegment = new ArraySegment<byte>(array, 0, 1);
            try
            {
                return ArraySegmentReceiveAsync(arraySegment, WebSocketCancelledToken).AsTask().Result;
            }
            finally
            {
                ArrayPool.Return(array);
            }
        }

#if !NETSTANDARD2_0
        private async ValueTask<int> MemoryReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            WebSocketCancelledToken.ThrowIfCancellationRequested();
            if (EndOfMessage) return 0;
            var result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            EndOfMessage = result.EndOfMessage;
            ByteCount += result.Count;
            return result.Count;
        }

        public override int Read(Span<byte> buffer) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            return await MemoryReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            var memory = ArrayPool.Rent(AspNetCoreWebSocket.WebSocketBufferSize);
            while (!EndOfMessage)
            {
                await ReadAsync(memory);
            }
        }
#else
        public override async ValueTask DisposeAsync()
        {
            var memory = ArrayPool.Rent(AspNetCoreWebSocket.WebSocketBufferSize);
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