using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Internal.Logging;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;
using Console = RESTable.Admin.Console;

namespace RESTable.WebSockets;

/// <inheritdoc cref="IWebSocket" />
/// <inheritdoc cref="ITraceable" />
/// <summary>
/// </summary>
public abstract class WebSocket : IWebSocket, IWebSocketInternal, IServiceProvider, ITraceable, IAsyncDisposable
{
    protected WebSocket(string webSocketId, RESTableContext context)
    {
        ProtocolHolder = null!; // Set on Open()
        LifetimeTask = null!; // Set on Open() where applicable
        _closeDescription = "";

        Id = webSocketId;
        WebSocketClosed = new CancellationTokenSource();
        Status = WebSocketStatus.Waiting;
        Context = context;
        JsonProvider = context.GetRequiredService<IJsonProvider>();
        WebSocketManager = context.GetRequiredService<WebSocketManager>();
    }

    public Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new())
    {
        var shell = Context
            .GetRequiredService<ResourceCollection>()
            .GetTerminalResource<Shell>();
        return DirectTo(shell, assignments, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DirectTo<T>(ITerminalResource<T> resource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new()) where T : Terminal
    {
        if (Status != WebSocketStatus.Open)
            throw new InvalidOperationException(
                $"Unable to send WebSocket with status '{Status}' to terminal '{resource.Name}'");
        if (resource is null)
            throw new ArgumentNullException(nameof(resource));
        var _resource = (TerminalResource<T>) resource;
        var newTerminal = await _resource.CreateTerminal(Context, cancellationToken, assignments).ConfigureAwait(false);
        await Context.WebSocket!.ConnectTo(newTerminal).ConfigureAwait(false);
        await newTerminal.OpenTerminal(cancellationToken).ConfigureAwait(false);
    }

    public Task Send(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken = new())
    {
        return SendBufferedInternal(data, asText, cancellationToken);
    }

    public ValueTask<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken)
    {
        return new ValueTask<Stream>(GetOutgoingMessageStream(asText, cancellationToken));
    }


    /// <inheritdoc />
    public Task SendText(string data, CancellationToken cancellationToken = new())
    {
        return SendBufferedInternal(data, true, cancellationToken);
    }

    public async Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, CancellationToken cancellationToken = new())
    {
        if (!PreCheck(result)) return;
        await SendResultInfo(result, timeElapsed, writeHeaders, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendException(Exception exception, CancellationToken cancellationToken = new())
    {
        var error = exception.AsError();
        error.SetContext(Context);
        await SendResult(error, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IWebSocketInternal.SendTextRaw(string textData, CancellationToken cancellationToken)
    {
        if (Status != WebSocketStatus.Open) return;
        await SendBuffered(textData, true, cancellationToken).ConfigureAwait(false);
        TotalSentBytesCount += Encoding.UTF8.GetByteCount(textData);
    }

    private async Task SendBufferedInternal(string data, bool asText, CancellationToken cancellationToken)
    {
        switch (Status)
        {
            case WebSocketStatus.Closed:
                throw new InvalidOperationException("Cannot send data to a closed WebSocket");
            case WebSocketStatus.Open:
            {
                await SendBuffered(data, asText, cancellationToken).ConfigureAwait(false);
                var bytesCount = Encoding.UTF8.GetByteCount(data);
                TotalSentBytesCount += bytesCount;
                if (TerminalConnection?.Resource?.Name != Console.TypeName)
                {
                    var logEvent = new WebSocketEvent
                    (
                        MessageType.WebSocketOutput,
                        this,
                        data,
                        bytesCount
                    );
                    await Console.Log(Context, logEvent).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    private async Task SendBufferedInternal(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken)
    {
        switch (Status)
        {
            case WebSocketStatus.Closed:
                throw new InvalidOperationException("Cannot send data to a closed WebSocket");
            case WebSocketStatus.Open:
            {
                await SendBuffered(data, asText, cancellationToken).ConfigureAwait(false);
                var bytesCount = data.Length;
                TotalSentBytesCount += bytesCount;
                if (TerminalConnection?.Resource?.Name != Console.TypeName)
                {
#if NETSTANDARD2_0
                    var content = Encoding.UTF8.GetString(data.ToArray());
#else
                    var content = Encoding.UTF8.GetString(data.Span);
#endif
                    var logEvent = new WebSocketEvent
                    (
                        MessageType.WebSocketOutput,
                        this,
                        content,
                        bytesCount
                    );
                    await Console.Log(Context, logEvent).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    private bool PreCheck(IResult result)
    {
        switch (Status)
        {
            case WebSocketStatus.Open: break;
            case var other:
                throw new InvalidOperationException($"Unable to send results to a WebSocket with status '{other}'");
        }
        if (result is WebSocketUpgradeSuccessful) return false;

        if (result is Content { IsLocked: true })
            throw new InvalidOperationException(
                "Unable to send a result that is already assigned to a Websocket streaming " +
                "job. Streaming results are locked, and can only be streamed once.");
        return true;
    }

    private async Task SendResultInfo(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, CancellationToken cancellationToken = new())
    {
        var info = result.Headers.Info;
        var errorInfo = result.Headers.Error;
        var timeInfo = "";
        if (timeElapsed is not null)
            timeInfo = $" ({timeElapsed.Value.TotalMilliseconds} ms)";
        var tail = "";
        if (info is not null)
            tail += $". {info}";
        if (errorInfo is not null)
            tail += $" (see {errorInfo})";
        await SendBufferedInternal($"{result.StatusCode.ToCode()}: {result.StatusDescription}{timeInfo}{tail}", true, cancellationToken)
            .ConfigureAwait(false);
        if (writeHeaders)
        {
            var headerData = JsonProvider.SerializeToUtf8Bytes(result.Headers, true, true);
            await SendBufferedInternal
            (
                headerData,
                true,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is WebSocket ws && ws.Id == Id;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    #region State

    private long BytesReceived { get; set; }
    private long TotalSentBytesCount { get; set; }
    private WebSocketConnection? TerminalConnection { get; set; }
    private IProtocolHolder ProtocolHolder { get; set; }
    private IJsonProvider JsonProvider { get; }
    private WebSocketManager WebSocketManager { get; }
    private bool _disposed;
    private string _closeDescription;

    internal Terminal? Terminal => TerminalConnection?.Terminal;

    internal AppProfile GetAppProfile()
    {
        return new(this);
    }

    private CancellationTokenSource WebSocketClosed { get; }

    protected void Cancel()
    {
        WebSocketClosed.Cancel();
    }

    /// <summary>
    ///     The ID of the WebSocket
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     The date and time when this WebSocket was opened
    /// </summary>
    public DateTime OpenedAt { get; private set; }

    /// <summary>
    ///     The date and time when this WebSocket was closed
    /// </summary>
    public DateTime ClosedAt { get; private set; }

    /// <summary>
    ///     The client connected to this WebSocket
    /// </summary>
    public Client Client => Context.Client;

    /// <inheritdoc />
    /// <summary>
    ///     The status of the WebSocket
    /// </summary>
    public WebSocketStatus Status { get; private set; }

    /// <inheritdoc />
    void IWebSocketInternal.SetStatus(WebSocketStatus status)
    {
        Status = status;
    }

    /// <inheritdoc />
    /// <summary>
    ///     The headers contained in the WebSocket upgrade request
    /// </summary>
    public Headers Headers => ProtocolHolder.Headers;

    /// <inheritdoc />
    /// <summary>
    ///     The cookies contained in the WebSocket upgrade request
    /// </summary>
    public ReadonlyCookies Cookies => Client.Cookies.AsReadonly();

    /// <inheritdoc />
    /// <summary>
    ///     The context in which this WebSocket was opened
    /// </summary>
    public RESTableContext Context { get; private set; }

    /// <summary>
    ///     A task representing the lifetime of this WebSocket comnection
    /// </summary>
    public Task LifetimeTask { get; private set; }

    /// <summary>
    ///     Is the WebSocket currently connected?
    /// </summary>
    protected abstract bool IsConnected { get; }

    /// <inheritdoc />
    public CancellationToken WebSocketAborted => WebSocketClosed.Token;

    #endregion

    #region Delegating members

    internal ITerminalResource? TerminalResource => TerminalConnection?.Resource;
    IContentTypeProvider IContentTypeHolder.InputContentTypeProvider => ProtocolHolder.InputContentTypeProvider;
    IContentTypeProvider IContentTypeHolder.OutputContentTypeProvider => ProtocolHolder.OutputContentTypeProvider;
    public bool ExcludeHeaders => ProtocolHolder.ExcludeHeaders;
    public string ProtocolIdentifier => ProtocolHolder.ProtocolIdentifier;
    public CachedProtocolProvider CachedProtocolProvider => ProtocolHolder.CachedProtocolProvider;

    public string? HeadersStringCache
    {
        get => ProtocolHolder.HeadersStringCache;
        set => ProtocolHolder.HeadersStringCache = value;
    }

    public object? GetService(Type serviceType)
    {
        return Context.GetService(serviceType);
    }

    #endregion

    #region Connection and terminal logic

    internal async Task<WebSocketConnection> ConnectTo(Terminal terminal)
    {
        await ReleaseTerminal().ConfigureAwait(false);
        var terminalConnection = new WebSocketConnection(this, terminal);
        TerminalConnection = terminalConnection;
        return terminalConnection;
    }

    private async Task ReleaseTerminal()
    {
        if (TerminalConnection is not null)
            await TerminalConnection.DisposeAsync().ConfigureAwait(false);
        TerminalConnection = null;
    }

    public async Task OpenAndAttachClientSocketToTerminal<T>(IProtocolHolder protocolHolder, T terminal, CancellationToken cancellationToken) where T : Terminal
    {
        ProtocolHolder = protocolHolder;
        Context = new WebSocketContext(this, Client, protocolHolder.Context);
        var connection = await ConnectTo(terminal).ConfigureAwait(false);
        await connection.Suspend().ConfigureAwait(false);
        var openTerminalTask = terminal.OpenTerminal(cancellationToken);
        await OpenServerWebSocket(cancellationToken).ConfigureAwait(false);
        connection.Unsuspend();
        await openTerminalTask.ConfigureAwait(false);
    }

    public async Task OpenAndAttachServerSocketToTerminal<T>
    (
        IProtocolHolder protocolHolder,
        ITerminalResource<T> terminalResource,
        IEnumerable<Condition<T>> assignments,
        CancellationToken cancellationToken
    )
        where T : class
    {
        ProtocolHolder = protocolHolder;
        Context = new WebSocketContext(this, Client, protocolHolder.Context);
        var terminalResourceInternal = (TerminalResource<T>) terminalResource;
        var terminal = await terminalResourceInternal.CreateTerminal(Context, cancellationToken, assignments).ConfigureAwait(false);
        await OpenServerWebSocket(cancellationToken).ConfigureAwait(false);
        await ConnectTo(terminal).ConfigureAwait(false);
        await terminal.OpenTerminal(cancellationToken).ConfigureAwait(false);
    }

    public async Task UseOnce(IProtocolHolder protocolHolder, Func<WebSocket, Task> action, CancellationToken cancellationToken)
    {
        ProtocolHolder = protocolHolder;
        Context = new WebSocketContext(this, Client, protocolHolder.Context);
        await using var webSocket = this;
        await Context.WebSocket!.OpenServerWebSocket(cancellationToken, false).ConfigureAwait(false);
        await action(this).ConfigureAwait(false);
    }

    /// <summary>
    ///     Connects the websocket and opens it for a terminal connection lifetime.
    /// </summary>
    private async Task OpenClientWebSocket(CancellationToken cancellationToken, bool acceptIncomingMessages = true)
    {
        switch (Status)
        {
            case WebSocketStatus.Waiting:
            {
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, WebSocketClosed.Token);
                if (acceptIncomingMessages)
                    LifetimeTask = InitMessageReceiveListener(cancellationTokenSource.Token);
                await ConnectUnderlyingWebSocket(cancellationToken).ConfigureAwait(false);
                Status = WebSocketStatus.Open;
                OpenedAt = DateTime.Now;
                if (TerminalConnection?.Resource?.Name != Console.TypeName) await Console.Log(Context, new WebSocketEvent(MessageType.WebSocketOpen, this)).ConfigureAwait(false);
                break;
            }
            default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
        }
    }

    /// <summary>
    ///     Sends the websocket upgrade and open this websocket for a terminal connection lifetime.
    /// </summary>
    private async Task OpenServerWebSocket(CancellationToken cancellationToken, bool acceptIncomingMessages = true)
    {
        switch (Status)
        {
            case WebSocketStatus.Waiting:
            {
                await ConnectUnderlyingWebSocket(cancellationToken).ConfigureAwait(false);
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, WebSocketClosed.Token);
                if (acceptIncomingMessages)
                    LifetimeTask = InitMessageReceiveListener(cancellationTokenSource.Token);
                Status = WebSocketStatus.Open;
                OpenedAt = DateTime.Now;
                if (TerminalConnection?.Resource?.Name != Console.TypeName) await Console.Log(Context, new WebSocketEvent(MessageType.WebSocketOpen, this)).ConfigureAwait(false);
                break;
            }
            default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
        }
    }

    #endregion

    #region Disconnection / dispose

    public string CloseDescription
    {
        get => _closeDescription;
        set
        {
#if NETSTANDARD2_0
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > 123)
                bytes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(bytes, 123));
            _closeDescription = Encoding.UTF8.GetString(bytes);
#else
            var maxChars = Encoding.UTF8.GetMaxByteCount(value.Length);
            Span<byte> byteBuffer = stackalloc byte[maxChars];
            var length = Encoding.UTF8.GetBytes(value, byteBuffer);
            var take = Math.Min(length, 123);
            _closeDescription = Encoding.UTF8.GetString(byteBuffer[..take]);
#endif
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Disposes the WebSocket and closes its connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        WebSocketManager.RemoveWebSocket(Id);
        Status = WebSocketStatus.PendingClose;
        var terminalName = TerminalConnection?.Resource?.Name;
        await ReleaseTerminal().ConfigureAwait(false);
        if (IsConnected)
            await Close(CloseDescription, CancellationToken.None).ConfigureAwait(false);
        WebSocketClosed.Cancel();
        Status = WebSocketStatus.Closed;
        ClosedAt = DateTime.Now;
        if (terminalName != Console.TypeName) await Console.Log(Context, new WebSocketEvent(MessageType.WebSocketClose, this)).ConfigureAwait(false);
        _disposed = true;
    }

    #endregion

    #region Protected API

    /// <summary>
    ///     Forwards the input text message to be handled by the connected terminal
    /// </summary>
    protected Task HandleTextInput(string textInput, CancellationToken cancellationToken)
    {
        return WebSocketManager.HandleTextInput(Id, textInput, cancellationToken);
    }

    /// <summary>
    ///     Forwards the input binary message to be handled by the connected terminal
    /// </summary>
    protected Task HandleBinaryInput(Stream binaryInput, CancellationToken cancellationToken)
    {
        return WebSocketManager.HandleBinaryInput(Id, binaryInput, cancellationToken);
    }

    /// <summary>
    ///     Send the buffered string data over the websocket as either text or binary
    /// </summary>
    protected abstract Task SendBuffered(string data, bool asText, CancellationToken cancellationToken);

    /// <summary>
    ///     Send the buffered binary data over the websocket as either text or binary
    /// </summary>
    protected abstract Task SendBuffered(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns a stream that, when written to, writes data over the websocket as a single
    ///     message until dispose, as either binary or text.
    /// </summary>
    protected abstract Stream GetOutgoingMessageStream(bool asText, CancellationToken cancellationToken);

    /// <summary>
    ///     Sends the WebSocket upgrade and initiates the actual underlying WebSocket connection
    /// </summary>
    protected abstract Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken);

    /// <summary>
    ///     Initiates a task that represents the lifetime of the WebSocket, handling incoming messages and
    ///     sending responses, and that is completed once the WebSocket is gracefully closed.
    /// </summary>
    /// <returns></returns>
    protected abstract Task InitMessageReceiveListener(CancellationToken cancellationToken);

    /// <summary>
    ///     Disconnects the actual underlying WebSocket connection
    /// </summary>
    protected abstract Task Close(string description, CancellationToken cancellationToken);

    #endregion

    #region Input

    /// <summary>
    ///     Handed back to the websocket once the websocket manager has validated the input
    /// </summary>
    internal async Task HandleTextInputInternal(string textData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TerminalConnection is null) return;
        if (!TerminalConnection.Terminal.SupportsTextInput)
        {
            await SendResult
            (
                new UnsupportedWebSocketInput($"Terminal '{TerminalConnection!.Resource!.Name}' does not support text input"),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return;
        }
        var byteCount = Encoding.UTF8.GetByteCount(textData);
        if (TerminalConnection.Resource?.Name != Console.TypeName)
        {
            var wsEvent = new WebSocketEvent
            (
                MessageType.WebSocketInput,
                this,
                textData,
                byteCount
            );
            await Console.Log(Context, wsEvent).ConfigureAwait(false);
        }
        await TerminalConnection.Terminal.HandleTextInput(textData, cancellationToken).ConfigureAwait(false);
        BytesReceived += byteCount;
    }

    internal async Task HandleBinaryInputInternal(Stream binaryData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TerminalConnection is null) return;
        if (!TerminalConnection.Terminal.SupportsBinaryInput)
        {
            await SendResult
            (
                new UnsupportedWebSocketInput($"Terminal '{TerminalConnection!.Resource!.Name}' does not support binary input"),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return;
        }
        await TerminalConnection.Terminal.HandleBinaryInput(binaryData, cancellationToken).ConfigureAwait(false);
        if (TerminalConnection.Resource?.Name != Console.TypeName)
        {
            var wsEvent = new WebSocketEvent
            (
                MessageType.WebSocketInput,
                this,
                null,
                binaryData.Position
            );
            await Console.Log(Context, wsEvent).ConfigureAwait(false);
        }
        TotalSentBytesCount += binaryData.Position;
    }

    #endregion
}
