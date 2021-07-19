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

namespace RESTable.WebSockets
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="ITraceable" />
    /// <summary>
    /// </summary>
    public abstract class WebSocket : IWebSocket, IWebSocketInternal, IServiceProvider, ITraceable, IAsyncDisposable
    {
        private long BytesReceived { get; set; }
        private long TotalSentBytesCount { get; set; }
        private WebSocketConnection? TerminalConnection { get; set; }
        private IProtocolHolder? ProtocolHolder { get; set; }
        private bool _disposed;

        internal ITerminalResource? TerminalResource => TerminalConnection?.Resource;
        internal Terminal? Terminal => TerminalConnection?.Terminal;
        internal AppProfile GetAppProfile() => new(this);
        private CancellationTokenSource WebSocketClosed { get; }
        internal void Cancel() => WebSocketClosed.Cancel();

        /// <summary>
        /// The ID of the WebSocket
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The date and time when this WebSocket was opened
        /// </summary>
        public DateTime OpenedAt { get; private set; }

        /// <summary>
        /// The date and time when this WebSocket was closed
        /// </summary>
        public DateTime ClosedAt { get; private set; }

        /// <summary>
        /// The client connected to this WebSocket
        /// </summary>
        public Client Client => Context.Client;

        /// <inheritdoc />
        /// <summary>
        /// The status of the WebSocket
        /// </summary>
        public WebSocketStatus Status { get; private set; }

        /// <inheritdoc />
        void IWebSocketInternal.SetStatus(WebSocketStatus status)
        {
            Status = status;
        }

        /// <inheritdoc />
        /// <summary>
        /// The headers contained in the WebSocket upgrade request
        /// </summary>
        public Headers Headers => ProtocolHolder.Headers;

        /// <inheritdoc />
        /// <summary>
        /// The cookies contained in the WebSocket upgrade request
        /// </summary>
        public ReadonlyCookies Cookies => Client.Cookies.AsReadonly();

        /// <inheritdoc />
        /// <summary>
        /// The context in which this WebSocket was opened
        /// </summary>
        public RESTableContext Context { get; private set; }

        public bool ExcludeHeaders => ProtocolHolder.ExcludeHeaders;
        public string ProtocolIdentifier => ProtocolHolder.ProtocolIdentifier;
        public CachedProtocolProvider CachedProtocolProvider => ProtocolHolder.CachedProtocolProvider;
        public Task LifetimeTask { get; private set; }

        public string? HeadersStringCache
        {
            get => ProtocolHolder.HeadersStringCache;
            set => ProtocolHolder.HeadersStringCache = value;
        }

        protected WebSocket(string webSocketId, RESTableContext context)
        {
            Id = webSocketId;
            WebSocketClosed = new CancellationTokenSource();
            Status = WebSocketStatus.Waiting;
            Context = context;
            JsonProvider = context.GetRequiredService<IJsonProvider>();
            WebSocketManager = context.GetRequiredService<WebSocketManager>();
        }

        public object? GetService(Type serviceType) => Context.GetService(serviceType);

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
            var terminal = await terminalResourceInternal.CreateTerminal(Context, assignments).ConfigureAwait(false);
            await OpenServerWebSocket(cancellationToken).ConfigureAwait(false);
            await ConnectTo(terminal).ConfigureAwait(false);
            await terminal.OpenTerminal(cancellationToken).ConfigureAwait(false);
        }

        public async Task UseOnce(IProtocolHolder protocolHolder, Func<WebSocket, Task> action, CancellationToken cancellationToken)
        {
            ProtocolHolder = protocolHolder;
            Context = new WebSocketContext(this, Client, protocolHolder.Context);
            await using var webSocket = this;
            await Context.WebSocket.OpenServerWebSocket(cancellationToken, false).ConfigureAwait(false);
            await action(this).ConfigureAwait(false);
        }

        /// <summary>
        /// Connects the websocket and opens it for a terminal connection lifetime.
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
                    if (TerminalConnection?.Resource?.Name != Admin.Console.TypeName)
                    {
                        await Admin.Console.Log(Context, new WebSocketEvent(MessageType.WebSocketOpen, this)).ConfigureAwait(false);
                    }
                    break;
                }
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        /// <summary>
        /// Sends the websocket upgrade and open this websocket for a terminal connection lifetime.
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
                    if (TerminalConnection?.Resource?.Name != Admin.Console.TypeName)
                    {
                        await Admin.Console.Log(Context, new WebSocketEvent(MessageType.WebSocketOpen, this)).ConfigureAwait(false);
                    }
                    break;
                }
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Disposes the WebSocket and closes its connection.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            WebSocketManager.RemoveWebSocket(Id);
            Status = WebSocketStatus.PendingClose;
            if (StreamManifest is not null)
                await StreamManifest.DisposeAsync().ConfigureAwait(false);
            StreamManifest = null;
            var terminalName = TerminalConnection?.Resource?.Name;
            await ReleaseTerminal().ConfigureAwait(false);
            if (IsConnected)
                await Close(CancellationToken.None).ConfigureAwait(false);
            WebSocketClosed.Cancel();
            Status = WebSocketStatus.Closed;
            ClosedAt = DateTime.Now;
            if (terminalName != Admin.Console.TypeName)
            {
                await Admin.Console.Log(Context, new WebSocketEvent(MessageType.WebSocketClose, this)).ConfigureAwait(false);
            }
            _disposed = true;
        }

        #region Protected API

        protected Task HandleTextInput(string textInput, CancellationToken cancellationToken)
        {
            return WebSocketManager.HandleTextInput(Id, textInput, cancellationToken);
        }

        protected Task HandleBinaryInput(Stream binaryInput, CancellationToken cancellationToken)
        {
            return WebSocketManager.HandleBinaryInput(Id, binaryInput, cancellationToken);
        }

        /// <summary>
        /// Is the WebSocket currently connected?
        /// </summary>
        protected abstract bool IsConnected { get; }

        /// <summary>
        /// Sends text data to the client over the WebSocket
        /// </summary>
        protected abstract Task Send(string text, CancellationToken token);

        /// <summary>
        /// Sends binary or text data to the client over the WebSocket
        /// </summary>
        protected abstract Task Send(ArraySegment<byte> data, bool asText, CancellationToken token);

        /// <summary>
        /// Sends binary or text data to the client over the WebSocket
        /// </summary>
        protected abstract Task<long> Send(Stream data, bool asText, CancellationToken token);

        /// <summary>
        /// Returns a stream that, when written to, writes data over the websocket
        /// </summary>
        protected abstract Task<Stream> GetOutgoingMessageStream(bool asText, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the WebSocket upgrade and initiates the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates a task that represents the lifetime of the WebSocket, handling incoming messages and
        /// sending responses, and that is completed once the WebSocket is gracefully closed.
        /// </summary>
        /// <returns></returns>
        protected abstract Task InitMessageReceiveListener(CancellationToken cancellationToken);

        /// <summary>
        /// Disconnects the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task Close(CancellationToken cancellationToken);

        #endregion

        #region IWebSocket

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
            var newTerminal = await _resource.CreateTerminal(Context, assignments).ConfigureAwait(false);
            await Context.WebSocket!.ConnectTo(newTerminal).ConfigureAwait(false);
            await newTerminal.OpenTerminal(cancellationToken).ConfigureAwait(false);
        }

        #region Input

        internal async Task HandleTextInputInternal(string textData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TerminalConnection is null) return;
            if (!TerminalConnection.Terminal.SupportsTextInputInternal)
            {
                await SendResult
                (
                    result: new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support text input"),
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                return;
            }
            if (TerminalConnection.Resource?.Name != Admin.Console.TypeName)
            {
                var wsEvent = new WebSocketEvent
                (
                    direction: MessageType.WebSocketInput,
                    webSocket: this,
                    content: textData,
                    length: Encoding.UTF8.GetByteCount(textData)
                );
                await Admin.Console.Log(Context, wsEvent).ConfigureAwait(false);
            }
            await TerminalConnection.Terminal.HandleTextInput(textData, cancellationToken).ConfigureAwait(false);
            BytesReceived += Encoding.UTF8.GetByteCount(textData);
        }

        internal async Task HandleBinaryInputInternal(Stream binaryData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TerminalConnection is null) return;
            if (!TerminalConnection.Terminal.SupportsBinaryInputInternal)
            {
                await SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support binary input"),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return;
            }
            await TerminalConnection.Terminal.HandleBinaryInput(binaryData, cancellationToken).ConfigureAwait(false);
            if (TerminalConnection.Resource?.Name != Admin.Console.TypeName)
            {
                var wsEvent = new WebSocketEvent
                (
                    direction: MessageType.WebSocketInput,
                    webSocket: this,
                    content: null,
                    length: binaryData.Position
                );
                await Admin.Console.Log(Context, wsEvent).ConfigureAwait(false);
            }
            TotalSentBytesCount += binaryData.Position;
        }

        #endregion

        #region Simple output

        public Task<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken) => GetOutgoingMessageStream(asText, cancellationToken);

        private async Task _SendString(string textData, CancellationToken cancellationToken)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    await Send(textData, cancellationToken).ConfigureAwait(false);
                    TotalSentBytesCount += Encoding.UTF8.GetByteCount(textData);
                    if (TerminalConnection?.Resource?.Name != Admin.Console.TypeName)
                    {
                        var logEvent = new WebSocketEvent
                        (
                            direction: MessageType.WebSocketOutput,
                            webSocket: this,
                            content: textData,
                            length: Encoding.UTF8.GetByteCount(textData)
                        );
                        await Admin.Console.Log(Context, logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        private async Task _SendArraySegment(ArraySegment<byte> binaryData, bool asText, CancellationToken cancellationToken)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    await Send(binaryData, asText, cancellationToken).ConfigureAwait(false);
                    TotalSentBytesCount += binaryData.Count;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                    {
                        var logEvent = new WebSocketEvent
                        (
                            direction: MessageType.WebSocketOutput,
                            webSocket: this,
                            content: Encoding.UTF8.GetString(binaryData.Array ?? Array.Empty<byte>()),
                            length: binaryData.Count
                        );
                        await Admin.Console.Log(Context, logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        private async Task _SendStream(Stream binaryData, bool asText, CancellationToken cancellationToken)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    var sentBytes = await Send(binaryData, asText, cancellationToken).ConfigureAwait(false);
                    TotalSentBytesCount += sentBytes;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                    {
                        var logEvent = new WebSocketEvent
                        (
                            direction: MessageType.WebSocketOutput,
                            webSocket: this,
                            content: null,
                            length: sentBytes
                        );
                        await Admin.Console.Log(Context, logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        /// <inheritdoc />
        public Task SendText(string data, CancellationToken cancellationToken = new()) => _SendString(data, cancellationToken);

        /// <inheritdoc />
        public Task SendText(ArraySegment<byte> data, CancellationToken cancellationToken = new()) => _SendArraySegment(data, true, cancellationToken);

        /// <inheritdoc />
        public Task SendText(Stream stream, CancellationToken cancellationToken = new()) => _SendStream(stream, true, cancellationToken);

        /// <inheritdoc />
        public Task SendBinary(ArraySegment<byte> data, CancellationToken cancellationToken = new()) => _SendArraySegment(data, false, cancellationToken);

        /// <inheritdoc />
        public Task SendBinary(Stream stream, CancellationToken cancellationToken = new()) => _SendStream(stream, false, cancellationToken);

        /// <inheritdoc />
        async Task IWebSocketInternal.SendTextRaw(string textData, CancellationToken cancellationToken)
        {
            if (Status != WebSocketStatus.Open) return;
            await Send(textData, cancellationToken).ConfigureAwait(false);
            TotalSentBytesCount += Encoding.UTF8.GetByteCount(textData);
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

            if (result is Content {IsLocked: true})
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
            await _SendString($"{result.StatusCode.ToCode()}: {result.StatusDescription}{timeInfo}{tail}", cancellationToken)
                .ConfigureAwait(false);
            if (writeHeaders)
                await SendJson(result.Headers, true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, CancellationToken cancellationToken = new())
        {
            if (!PreCheck(result)) return;
            await SendResultInfo(result, timeElapsed, writeHeaders, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SendSerializedResult
        (
            ISerializedResult serializedResult,
            TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true,
            CancellationToken cancellationToken = new()
        )
        {
            try
            {
                await SendResult(serializedResult.Result, timeElapsed, writeHeaders, cancellationToken).ConfigureAwait(false);
                await SendBinary(serializedResult.Body, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (disposeResult)
                    await serializedResult.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task SendException(Exception exception, CancellationToken cancellationToken = new())
        {
            var error = exception.AsError();
            error.SetContext(Context);
            await SendResult(error, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private IJsonProvider JsonProvider { get; }
        private WebSocketManager WebSocketManager { get; }

        /// <inheritdoc />
        public async Task SendJson(object dataObject, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new())
        {
            if (dataObject is null) throw new ArgumentNullException(nameof(dataObject));
            var message = await GetMessageStream(asText, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (message)
#else
            await using (message.ConfigureAwait(false))
#endif
            {
                JsonProvider.SerializeToStream(message, dataObject, prettyPrint, ignoreNulls);
            }
        }

        #endregion

        #region Streaming

        internal async Task HandleStreamingTextInput(string textInput, CancellationToken cancellationToken)
        {
            var (command, arg) = textInput.TupleSplit(' ');
            switch (command.ToUpperInvariant())
            {
                case "OPTIONS":
                case "MANIFEST":
                    await SendManifest(cancellationToken).ConfigureAwait(false);
                    break;
                case "GET":
                    await StreamMessage(-1, cancellationToken).ConfigureAwait(false);
                    break;
                case "NEXT" when int.TryParse(arg, out var nr) && nr > 0:
                    await StreamMessage(nr, cancellationToken).ConfigureAwait(false);
                    break;
                case "NEXT":
                    await StreamMessage(1, cancellationToken).ConfigureAwait(false);
                    break;
                case "CLOSE":
                    await CloseStream(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        internal bool IsStreaming => StreamManifest is not null;
        private StreamManifest? StreamManifest { get; set; }

        private const int MaxStreamBufferSize = 16_000_000;
        private const int MinStreamBufferSize = 512;

        /// <inheritdoc />
        public async Task StreamSerializedResult
        (
            ISerializedResult serializedResult,
            int messageSize,
            TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true,
            CancellationToken cancellationToken = new()
        )
        {
            if (serializedResult is null) throw new ArgumentNullException(nameof(serializedResult));
            var content = serializedResult.Result as Content;

            if (content is null || !(serializedResult.Body?.Length > 0))
            {
                await SendSerializedResult(serializedResult, serializedResult.TimeElapsed, writeHeaders, disposeResult, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
            if (content.IsLocked)
                throw new InvalidOperationException(
                    "Unable to stream a result that is already assigned to a different streaming " +
                    "job. A result can only be streamed once.");
            content.IsLocked = true;
            if (TerminalConnection is not null)
                await TerminalConnection.Suspend().ConfigureAwait(false);
            if (messageSize < MinStreamBufferSize)
                messageSize = MinStreamBufferSize;
            else if (MaxStreamBufferSize < messageSize)
                messageSize = MaxStreamBufferSize;
            if (StreamManifest is not null)
                await StreamManifest.DisposeAsync().ConfigureAwait(false);
            StreamManifest = new StreamManifest(serializedResult, messageSize);
            await SendJson(StreamManifest, cancellationToken: cancellationToken).ConfigureAwait(false);
            buffer = null;
        }

        private byte[] buffer;

        private async Task CloseStream(CancellationToken cancellationToken)
        {
            await SendText($"499: Client closed request. Streamed {StreamManifest.CurrentMessageIndex} " +
                           $"of {StreamManifest.NrOfMessages} messages.", cancellationToken).ConfigureAwait(false);
            StopStreaming();
        }

        private void StopStreaming()
        {
            StreamManifest.Dispose();
            StreamManifest = null;
            buffer = null;
            TerminalConnection?.Unsuspend();
        }

        private async Task SendManifest(CancellationToken cancellationToken) => await SendJson(StreamManifest, cancellationToken: cancellationToken).ConfigureAwait(false);

        private async Task StreamMessage(int nr, CancellationToken cancellationToken = new())
        {
            try
            {
                if (nr == -1) nr = StreamManifest.MessagesRemaining;
                var endIndex = StreamManifest.CurrentMessageIndex + nr;
                if (endIndex > StreamManifest.NrOfMessages)
                    endIndex = StreamManifest.NrOfMessages;
                buffer ??= new byte[StreamManifest.BufferSize];
                while (StreamManifest.CurrentMessageIndex < endIndex)
                {
                    var read = StreamManifest.Result.Body.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    var message = StreamManifest.Messages[StreamManifest.CurrentMessageIndex];
                    await read.ConfigureAwait(false);
                    await SendBinary(new ArraySegment<byte>(buffer, 0, (int) message.Length), cancellationToken).ConfigureAwait(false);
                    message.IsSent = true;
                    StreamManifest.MessagesStreamed += 1;
                    StreamManifest.MessagesRemaining -= 1;
                    StreamManifest.BytesStreamed += message.Length;
                    StreamManifest.BytesRemaining -= message.Length;
                    StreamManifest.CurrentMessageIndex += 1;
                }
                if (StreamManifest.Messages[StreamManifest.LastIndex].IsSent)
                {
                    await SendText($"200: OK. {StreamManifest.NrOfMessages} messages sucessfully streamed.", cancellationToken)
                        .ConfigureAwait(false);
                    StopStreaming();
                }
            }
            catch (Exception e)
            {
                await SendException(e, cancellationToken).ConfigureAwait(false);
                await SendText(
                    $"500: Error during streaming. Streamed {StreamManifest?.CurrentMessageIndex ?? 0} " +
                    $"of {StreamManifest?.NrOfMessages ?? 1} messages.", cancellationToken).ConfigureAwait(false);
                StopStreaming();
            }
        }

        #endregion

        #endregion

        /// <inheritdoc />
        public override string ToString() => Id;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is WebSocket ws && ws.Id == Id;

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();
    }
}