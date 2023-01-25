using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.WebSockets;

internal sealed class CombinedWebSocketsMessageStream : Stream, IAsyncDisposable
{
    private WebSocketMessageType MessageType { get; }
    private CancellationToken CancellationToken { get; }
    private Stream[] MessageStreams { get; }
    private int StreamsLength { get; }
    private ArrayPool<ValueTask> TaskArrayPool { get; }
    private long ByteCount { get; set; }
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    private bool IsDisposed { get; set; }

    public override long Position
    {
        get => ByteCount;
        set => throw new NotSupportedException();
    }

    public CombinedWebSocketsMessageStream(Stream[] messageStreams, bool asText, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MessageStreams = messageStreams;
        StreamsLength = messageStreams.Length;
        MessageType = asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
        CancellationToken = cancellationToken;
        TaskArrayPool = ArrayPool<ValueTask>.Shared;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed websocket message stream");
        CancellationToken.ThrowIfCancellationRequested();
        var tasks = TaskArrayPool.Rent(StreamsLength);
        try
        {
            for (var i = 0; i < StreamsLength; i += 1)
                tasks[i] = MessageStreams[i].WriteAsync(buffer, cancellationToken);
            for (var i = 0; i < StreamsLength; i += 1)
                await tasks[i].ConfigureAwait(false);
            ByteCount += buffer.Length;
        }
        finally { TaskArrayPool.Return(tasks); }
    }

    public override async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        CancellationToken.ThrowIfCancellationRequested();
        var tasks = TaskArrayPool.Rent(StreamsLength);
        try
        {
            for (var i = 0; i < StreamsLength; i += 1)
                tasks[i] = MessageStreams[i].DisposeAsync();
            for (var i = 0; i < StreamsLength; i += 1)
                await tasks[i].ConfigureAwait(false);
        }
        finally { TaskArrayPool.Return(tasks); }
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override long Length => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
