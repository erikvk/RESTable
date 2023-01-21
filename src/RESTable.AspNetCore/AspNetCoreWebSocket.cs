using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using static System.Net.WebSockets.WebSocketMessageType;
using WebSocket = RESTable.WebSockets.WebSocket;

namespace RESTable.AspNetCore;

internal abstract class AspNetCoreWebSocket : WebSocket
{
    private const int MaxNumberOfConcurrentWriters = 1;

    /// <summary>
    ///     Ensures that at most one thread sends a message frame over this websocket at any one time, to
    ///     avoid frame fragmentation.
    /// </summary>
    private SemaphoreSlim SendMessageSemaphore { get; }

    protected System.Net.WebSockets.WebSocket? WebSocket { get; set; }
    private ArrayPool<byte> ArrayPool { get; }
    private int WebSocketBufferSize { get; }

    protected AspNetCoreWebSocket(string webSocketId, RESTableContext context) : base(webSocketId, context)
    {
        WebSocketBufferSize = context.Configuration.WebSocketBufferSize;
        ArrayPool = ArrayPool<byte>.Create(WebSocketBufferSize, 32);
        SendMessageSemaphore = new SemaphoreSlim(MaxNumberOfConcurrentWriters, MaxNumberOfConcurrentWriters);
    }

    protected override async Task SendBuffered(string data, bool asText, CancellationToken cancellationToken)
    {
        await SendMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var byteData = buffer.AsMemory();
            await WebSocket!.SendAsync(byteData, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SendMessageSemaphore.Release();
        }
    }

    protected override async Task SendBuffered(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken)
    {
        await SendMessageSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WebSocket!.SendAsync(data, asText ? Text : Binary, true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SendMessageSemaphore.Release();
        }
    }

    protected override Stream GetOutgoingMessageStream(bool asText, CancellationToken cancellationToken)
    {
        return new AspNetCoreOutputMessageStream
        (
            WebSocket!,
            asText ? Text : Binary,
            SendMessageSemaphore,
            cancellationToken
        );
    }

    protected abstract override Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken);

    private static async ValueTask<(WebSocketMessageType messageType, bool endOfMessage, int byteCount)> GetInitial
    (
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken
    )
    {
        var initial = await webSocket.ReceiveAsync(Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
        return (initial.MessageType, initial.EndOfMessage, initial.Count);
    }

    protected override async Task InitMessageReceiveListener(CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                while (!WebSocket!.CloseStatus.HasValue)
                {
                    var (messageType, endOfMessage, byteCount) = await GetInitial(WebSocket, cancellationToken).ConfigureAwait(false);
                    switch (messageType)
                    {
                        case Binary:
                        {
                            var nextMessage = new AspNetCoreInputMessageStream(WebSocket, messageType, endOfMessage, byteCount, ArrayPool, WebSocketBufferSize,
                                cancellationToken);
                            await using (nextMessage.ConfigureAwait(false))
                            {
                                await HandleBinaryInput(nextMessage, cancellationToken).ConfigureAwait(false);
                            }
                            break;
                        }
                        case Text:
                        {
                            var nextMessage = new AspNetCoreInputMessageStream(WebSocket, messageType, endOfMessage, byteCount, ArrayPool, WebSocketBufferSize,
                                cancellationToken);
                            Task handleTask;
                            await using (nextMessage.ConfigureAwait(false))
                            {
                                using (var reader = new StreamReader(nextMessage, Encoding.Default, true, WebSocketBufferSize, true))
                                {
                                    var stringMessage = await reader.ReadToEndAsync().ConfigureAwait(false);
                                    handleTask = HandleTextInput(stringMessage, cancellationToken);
                                }
                            }
                            await handleTask.ConfigureAwait(false);
                            break;
                        }
                    }
                }
            }
            catch (WebSocketException)
            {
                // Client closed without completing close handshake
            }
            catch (Exception exception)
            {
                // An exception was thrown from the terminal, or when handling websocket messages. Let's 
                // collect some information about the error and set the close description, before returning 
                // from the lifetimetask.
                var error = exception.AsError();
                CloseDescription = await error.GetLogMessage().ConfigureAwait(false);
            }
            finally
            {
                // WebSocket is closing, so we cancel all tasks depending on its cancellation token.
                Cancel();
            }
        }
        // Catch cancellation exceptions before returning
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
    }

    protected override async Task TryClose(string description, CancellationToken cancellationToken)
    {
        if (WebSocket is null)
            return;
        using (WebSocket)
        {
            if (WebSocket?.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, description, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
