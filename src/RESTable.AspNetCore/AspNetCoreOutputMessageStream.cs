using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.AspNetCore;

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

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
        if (WebSocket.State is not WebSocketState.Open)
            throw new OperationCanceledException();
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken).Token;
        combinedToken.ThrowIfCancellationRequested();
        if (!SemaphoreOpen)
        {
            await WriteSemaphore.WaitAsync(combinedToken).ConfigureAwait(false);
            SemaphoreOpen = true;
        }
        await WebSocket.SendAsync
        (
            buffer,
            MessageType,
            false,
            combinedToken
        ).ConfigureAwait(false);
        ByteCount += buffer.Length;
    }

    public override async ValueTask DisposeAsync()
    {
        if
        (
            !SemaphoreOpen ||
            IsDisposed ||
            WebSocket.State is not WebSocketState.Open
        )
        {
            return;
        }
        try
        {
            await WebSocket.SendAsync
            (
                ReadOnlyMemory<byte>.Empty,
                MessageType,
                true,
                WebSocketCancelledToken
            ).ConfigureAwait(false);
            IsDisposed = true;
        }
        catch (WebSocketException)
        {
            // Ignore WebSocket exceptions
        }
        finally
        {
            WriteSemaphore.Release();
            SemaphoreOpen = false;
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer) => WriteAsync(buffer.ToArray(), CancellationToken.None).AsTask().Wait();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
