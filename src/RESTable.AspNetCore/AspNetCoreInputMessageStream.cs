﻿using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocket = System.Net.WebSockets.WebSocket;

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

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken);
        combinedTokenSource.Token.ThrowIfCancellationRequested();
        if (EndOfMessage) return 0;
        var result = await WebSocket.ReceiveAsync(buffer, combinedTokenSource.Token).ConfigureAwait(false);
        if (result.MessageType is WebSocketMessageType.Close)
        {
            throw new OperationCanceledException();
        }
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
            {
                // Read the rest of the message
                var _ = await ReadAsync(memory, WebSocketCancelledToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool.Return(arrayBuffer);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
