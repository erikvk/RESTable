using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore;

internal sealed class AspNetCoreInputMessageStream : AspNetCoreMessageStream, IAsyncDisposable
{
    public override bool CanRead => true;
    public override bool CanWrite => false;
    public override bool CanSeek => false;
    private bool EndOfMessage { get; set; }
    private int BufferSize { get; }

    public override long Position
    {
        get => ByteCount;
        set => throw new NotSupportedException();
    }

    private ArrayPool<byte> ArrayPool { get; }

    public AspNetCoreInputMessageStream
    (
        WebSocket webSocket,
        WebSocketMessageType messageType,
        bool endOfMessage,
        int initialByteCount,
        ArrayPool<byte> arrayPool,
        int bufferSize,
        CancellationToken webSocketCancelledToken
    ) : base
    (
        webSocket,
        messageType,
        webSocketCancelledToken
    )
    {
        BufferSize = bufferSize;
        ArrayPool = arrayPool;
        EndOfMessage = endOfMessage;
        ByteCount = initialByteCount;
    }

#if NETSTANDARD2_0
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        WebSocketCancelledToken.ThrowIfCancellationRequested();
        if (EndOfMessage) return 0;
        var arraySegment = new ArraySegment<byte>(buffer, offset, count);
        var result = await WebSocket.ReceiveAsync(arraySegment, cancellationToken).ConfigureAwait(false);
        if (result.MessageType != MessageType)
            throw new InvalidOperationException($"Received frame with type '{result.MessageType}' when expecting '{MessageType}'");
        EndOfMessage = result.EndOfMessage;
        ByteCount += result.Count;
        return result.Count;
    }

    public override async ValueTask DisposeAsync()
    {
        if (EndOfMessage || WebSocket.State != WebSocketState.Open)
            return;
        var arrayBuffer = ArrayPool.Rent(BufferSize);
        try
        {
            while (!EndOfMessage)
            {
                // Read the rest of the message
                await ReadAsync(arrayBuffer, 0, BufferSize);
            }
        }
        finally
        {
            ArrayPool.Return(arrayBuffer);
        }
    }
#else
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        WebSocketCancelledToken.ThrowIfCancellationRequested();
        if (EndOfMessage) return 0;
        var result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (result.MessageType != MessageType)
            throw new InvalidOperationException($"Received frame with type '{result.MessageType}' when expecting '{MessageType}'");
        EndOfMessage = result.EndOfMessage;
        ByteCount += result.Count;
        return result.Count;
    }

    public override async ValueTask DisposeAsync()
    {
        if (EndOfMessage || WebSocket.State != WebSocketState.Open)
            return;
        var arrayBuffer = ArrayPool.Rent(BufferSize);
        try
        {
            var memory = arrayBuffer.AsMemory(..BufferSize);
            while (!EndOfMessage)
                // Read the rest of the message
                await ReadAsync(memory);
        }
        finally
        {
            ArrayPool.Return(arrayBuffer);
        }
    }
#endif

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
