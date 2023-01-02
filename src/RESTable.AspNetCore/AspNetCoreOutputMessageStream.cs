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

#if NETSTANDARD2_0
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
        if (!SemaphoreOpen)
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken).Token;
            await WriteSemaphore.WaitAsync(combinedToken).ConfigureAwait(false);
            SemaphoreOpen = true;
        }
        WebSocketCancelledToken.ThrowIfCancellationRequested();
        await WebSocket.SendAsync
        (
            buffer: new ArraySegment<byte>(buffer, offset, count),
            messageType: MessageType,
            endOfMessage: false,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        ByteCount += count;
    }

    public override async ValueTask DisposeAsync()
    {
        if (!SemaphoreOpen || IsDisposed) return;
        if (WebSocketCancelledToken.IsCancellationRequested)
        {
            // Not much we can do in this case. The WebSocket is closed, so we can't send
            // the final frame.
            return;
        }
        try
        {
            await WebSocket.SendAsync
            (
                buffer: new ArraySegment<byte>(Array.Empty<byte>()),
                messageType: MessageType,
                endOfMessage: true,
                cancellationToken: WebSocketCancelledToken
            ).ConfigureAwait(false);
            IsDisposed = true;
        }
        finally
        {
            WriteSemaphore.Release();
            SemaphoreOpen = false;
        }
    }

    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, CancellationToken.None).Wait();
#else
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
    {
        if (IsDisposed)
            throw new InvalidOperationException("Cannot write to a closed WebSocket message stream");
        if (!SemaphoreOpen)
        {
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(WebSocketCancelledToken, cancellationToken).Token;
            await WriteSemaphore.WaitAsync(combinedToken).ConfigureAwait(false);
            SemaphoreOpen = true;
        }
        WebSocketCancelledToken.ThrowIfCancellationRequested();
        await WebSocket.SendAsync
        (
            buffer,
            MessageType,
            false,
            cancellationToken
        ).ConfigureAwait(false);
        ByteCount += buffer.Length;
    }

    public override async ValueTask DisposeAsync()
    {
        if (!SemaphoreOpen || IsDisposed) return;
        switch (WebSocket.State)
        {
            case WebSocketState.None:
            case WebSocketState.Connecting:
            case WebSocketState.CloseSent:
            case WebSocketState.Closed:
            case WebSocketState.Aborted: return;
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
        finally
        {
            WriteSemaphore.Release();
            SemaphoreOpen = false;
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer) => WriteAsync(buffer.ToArray(), CancellationToken.None).AsTask().Wait();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

#endif

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
