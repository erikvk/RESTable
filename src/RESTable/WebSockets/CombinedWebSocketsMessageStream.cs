using System;
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
        MessageType = asText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
        CancellationToken = cancellationToken;
    }

    private Task ForAll(Func<Stream, ValueTask> action)
    {
        CancellationToken.ThrowIfCancellationRequested();
        var taskArray = new Task[MessageStreams.Length];
        for (var i = 0; i < MessageStreams.Length; i += 1)
        {
            var stream = MessageStreams[i];
            taskArray[i] = action(stream).AsTask();
        }
        return Task.WhenAll(taskArray);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed websocket message stream");
        await ForAll(stream => stream.WriteAsync(buffer, cancellationToken)).ConfigureAwait(false);
        ByteCount += buffer.Length;
    }

    public override async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;
        await ForAll(stream => stream.DisposeAsync()).ConfigureAwait(false);
        IsDisposed = true;
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override long Length => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
