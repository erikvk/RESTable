using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RESTable.WebSockets;
using WebSocket = System.Net.WebSockets.WebSocket;

namespace RESTable.AspNetCore;

internal sealed class AspNetCoreOutputMessageStream : AspNetCoreMessageStream, IAsyncDisposable
{
    public override bool CanRead => false;
    public override bool CanWrite => true;

    private WebSocketMessageStreamMode Mode { get; }
    private SemaphoreSlim WriteSemaphore { get; }
    private bool SemaphoreOpen { get; set; }

    public override long Position
    {
        get => ByteCount;
        set => throw new NotSupportedException();
    }

    public AspNetCoreOutputMessageStream
    (
        WebSocket webSocket,
        WebSocketMessageStreamMode mode,
        WebSocketMessageType messageType,
        SemaphoreSlim writeSemaphore,
        CancellationToken webSocketCancelledToken
    )
        : base(webSocket, messageType, webSocketCancelledToken)
    {
        WriteSemaphore = writeSemaphore;
        Mode = mode;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
        if (WebSocket.State is not WebSocketState.Open)
        {
            if (Mode is WebSocketMessageStreamMode.Strict)
                throw new OperationCanceledException();
            return;
        }
        using var combindTokenSource = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken);
        var combinedToken = combindTokenSource.Token;
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

    public override void Write(ReadOnlySpan<byte> buffer) => WriteAsync(buffer.ToArray(), WebSocketCancelledToken).AsTask().Wait();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
