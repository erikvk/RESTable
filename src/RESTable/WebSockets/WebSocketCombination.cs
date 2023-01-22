using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets;

internal class WebSocketCombination : IWebSocket, IAsyncDisposable
{
    public WebSocketCombination(IWebSocket[] webSockets)
    {
        WebSockets = webSockets;
    }

    private IWebSocket[] WebSockets { get; }

    public WebSocketStatus Status => WebSocketStatus.Open;

    public Task SendText(string data, CancellationToken cancellationToken = new())
    {
        return DoForAll(ws => ws.SendText(data, cancellationToken));
    }

    public Task Send(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken = new())
    {
        return DoForAll(ws => ws.Send(data, asText, cancellationToken));
    }

    public Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, CancellationToken cancellationToken = new())
    {
        return DoForAll(ws => ws.SendResult(result, timeElapsed, writeHeaders, cancellationToken));
    }

    public Task SendException(Exception exception, CancellationToken cancellationToken = new())
    {
        return DoForAll(ws => ws.SendException(exception, cancellationToken));
    }

    public Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new())
    {
        return DoForAll(ws => ws.DirectToShell(assignments, cancellationToken));
    }

    public async ValueTask<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new())
    {
        var streams = new Stream[WebSockets.Length];
        for (var i = 0; i < WebSockets.Length; i += 1)
        {
            streams[i] = await WebSockets[i].GetMessageStream(asText, cancellationToken).ConfigureAwait(false);
        }
        return new CombinedWebSocketsMessageStream(streams, asText, cancellationToken);
    }

    public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new())
        where T : Terminal
    {
        return DoForAll(ws => ws.DirectTo(terminalResource, assignments, cancellationToken));
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    private Task DoForAll(Func<IWebSocket, Task> action)
    {
        var tasks = new Task[WebSockets.Length];
        for (var i = 0; i < WebSockets.Length; i += 1)
        {
            var socket = WebSockets[i];
            tasks[i] = action(socket);
        }
        return Task.WhenAll(tasks);
    }

    #region Not supported for WebSocketCombination

    public RESTableContext Context => throw new NotSupportedException();
    public Headers Headers => throw new NotSupportedException();

    public string? HeadersStringCache
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public bool ExcludeHeaders => throw new NotSupportedException();
    public string ProtocolIdentifier => throw new NotSupportedException();
    public CachedProtocolProvider CachedProtocolProvider => throw new NotSupportedException();
    public ReadonlyCookies Cookies => throw new NotSupportedException();
    IContentTypeProvider IContentTypeHolder.InputContentTypeProvider => throw new NotSupportedException();
    IContentTypeProvider IContentTypeHolder.OutputContentTypeProvider => throw new NotSupportedException();
    public CancellationToken WebSocketAborted => throw new NotSupportedException();
    public string CloseDescription => throw new NotSupportedException();

    #endregion
}
