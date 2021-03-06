﻿using System;
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
        public Client Client { get; }

        /// <inheritdoc />
        /// <summary>
        /// The status of the WebSocket
        /// </summary>
        public WebSocketStatus Status { get; private set; }

        /// <inheritdoc />
        public Task SetStatus(WebSocketStatus status)
        {
            Status = status;
            return Task.CompletedTask;
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

        private long BytesReceived { get; set; }
        private long TotalSentBytesCount { get; set; }

        internal ITerminalResource TerminalResource => TerminalConnection?.Resource;
        internal Terminal Terminal => TerminalConnection?.Terminal;

        private WebSocketConnection TerminalConnection { get; set; }

        internal AppProfile GetAppProfile() => new(this);

        public Task LifetimeTask { get; private set; }

        public object GetService(Type serviceType) => Context.Services.GetService(serviceType);

        internal async Task ConnectTo(Terminal terminal, ITerminalResource resource)
        {
            await ReleaseTerminal().ConfigureAwait(false);
            TerminalConnection = new WebSocketConnection(this, terminal, resource);
        }

        private async Task ReleaseTerminal()
        {
            if (TerminalConnection != null)
                await TerminalConnection.DisposeAsync().ConfigureAwait(false);
            TerminalConnection = null;
        }

        /// <summary>
        /// A cancellation token source that cancels when this Websocket is closed
        /// </summary>
        private CancellationTokenSource CancellationTokenSource { get; }

        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        /// <summary>
        /// Sends the websocket upgrade and open this websocket for a single transfer or
        /// a terminal connection lifetime.
        /// </summary>
        internal async Task Open(IRequest upgradeRequest, bool acceptIncomingMessages = true)
        {
            ProtocolHolder = upgradeRequest;
            Context = new WebSocketContext(this, Client, upgradeRequest);

            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    await SendUpgrade().ConfigureAwait(false);
                    if (acceptIncomingMessages)
                        LifetimeTask = InitMessageReceiveListener(CancellationTokenSource.Token);
                    Status = WebSocketStatus.Open;
                    OpenedAt = DateTime.Now;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                        await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketOpen, this))
                            .ConfigureAwait(false);
                    break;
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        private HashSet<Task> OngoingTasks { get; }

        private void RunWebSocketMessageTask(Task toRun)
        {
            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            async Task RunTask()
            {
                try
                {
                    await toRun.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // We're currently disposing this websocket
                }
                catch (Exception e)
                {
                    await Send(e.Message, CancellationTokenSource.Token).ConfigureAwait(false);
                    await DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    OngoingTasks.Remove(toRun);
                }
            }

            OngoingTasks.Add(RunTask());
        }

        protected void HandleTextInput(string textInput)
        {
            RunWebSocketMessageTask(WebSocketController.HandleTextInput(Id, textInput, CancellationTokenSource.Token));
        }

        protected void HandleBinaryInput(byte[] binaryInput)
        {
            RunWebSocketMessageTask(WebSocketController.HandleBinaryInput(Id, binaryInput, CancellationTokenSource.Token));
        }

        private IProtocolHolder ProtocolHolder { get; set; }

        public string HeadersStringCache
        {
            get => ProtocolHolder.HeadersStringCache;
            set => ProtocolHolder.HeadersStringCache = value;
        }

        public bool ExcludeHeaders => ProtocolHolder.ExcludeHeaders;
        public string ProtocolIdentifier => ProtocolHolder.ProtocolIdentifier;
        public CachedProtocolProvider CachedProtocolProvider => ProtocolHolder.CachedProtocolProvider;

        private bool disposed;

        /// <inheritdoc />
        /// <summary>
        /// Disposes the WebSocket. Same as Disconnect()
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            CancellationTokenSource.Cancel();
            await Task.WhenAll(OngoingTasks);
            WebSocketController.RemoveWebSocket(Id);
            Status = WebSocketStatus.PendingClose;
            if (StreamManifest != null)
                await StreamManifest.DisposeAsync().ConfigureAwait(false);
            StreamManifest = null;
            var terminalName = TerminalConnection?.Resource?.Name;
            await ReleaseTerminal().ConfigureAwait(false);
            if (IsConnected)
                await Close().ConfigureAwait(false);
            Status = WebSocketStatus.Closed;
            ClosedAt = DateTime.Now;
            if (terminalName != Admin.Console.TypeName)
                await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketClose, this)).ConfigureAwait(false);
            disposed = true;
        }

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
        protected abstract Task<Stream> GetOutgoingMessageStream(bool asText, CancellationToken token);

        public Task<Stream> GetMessageStream(bool asText) => GetOutgoingMessageStream(asText, CancellationTokenSource.Token);

        /// <summary>
        /// Is the WebSocket currently connected?
        /// </summary>
        protected abstract bool IsConnected { get; }

        /// <summary>
        /// Sends the WebSocket upgrade and initiates the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task SendUpgrade();

        /// <summary>
        /// Initiates a task that represents the lifetime of the WebSocket, handling incoming messages and
        /// sending responses, and that is completed once the WebSocket is gracefully closed.
        /// </summary>
        /// <returns></returns>
        protected abstract Task InitMessageReceiveListener(CancellationToken cancellationToken);

        /// <summary>
        /// Disconnects the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task Close();

        #region IWebSocket

        /// <inheritdoc />
        public Task DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => DirectTo(Shell.TerminalResource);

        /// <inheritdoc />
        public async Task DirectTo<T>(ITerminalResource<T> resource, ICollection<Condition<T>> assignments = null) where T : Terminal
        {
            if (Status != WebSocketStatus.Open)
                throw new InvalidOperationException(
                    $"Unable to send WebSocket with status '{Status}' to terminal '{resource.Name}'");
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(Context, assignments);
            await Context.WebSocket.ConnectTo(newTerminal, _resource).ConfigureAwait(false);
            await newTerminal.OpenTerminal().ConfigureAwait(false);
        }

        #region Input

        internal async Task HandleTextInputInternal(string textData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsTextInputInternal)
            {
                await SendResult(
                        new UnsupportedWebSocketInput(
                            $"Terminal '{TerminalConnection.Resource.Name}' does not support text input"))
                    .ConfigureAwait(false);
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
                await Admin.Console.Log(wsEvent).ConfigureAwait(false);
            }
            await TerminalConnection.Terminal.HandleTextInput(textData).ConfigureAwait(false);
            BytesReceived += Encoding.UTF8.GetByteCount(textData);
        }

        internal async Task HandleBinaryInputInternal(byte[] binaryData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsBinaryInputInternal)
            {
                await SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support binary input")).ConfigureAwait(false);
                return;
            }
            if (TerminalConnection.Resource?.Name != Admin.Console.TypeName)
            {
                var wsEvent = new WebSocketEvent
                (
                    direction: MessageType.WebSocketInput,
                    webSocket: this,
                    content: Encoding.UTF8.GetString(binaryData),
                    length: binaryData.LongLength
                );
                await Admin.Console.Log(wsEvent).ConfigureAwait(false);
            }
            await TerminalConnection.Terminal.HandleBinaryInput(binaryData).ConfigureAwait(false);
            TotalSentBytesCount += binaryData.Length;
        }

        #endregion

        #region Simple output

        private async Task _SendString(string textData)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    await Send(textData, CancellationTokenSource.Token).ConfigureAwait(false);
                    TotalSentBytesCount += Encoding.UTF8.GetByteCount(textData);
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                    {
                        var logEvent = new WebSocketEvent
                        (
                            direction: MessageType.WebSocketOutput,
                            webSocket: this,
                            content: textData,
                            length: Encoding.UTF8.GetByteCount(textData)
                        );
                        await Admin.Console.Log(logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        private async Task _SendArraySegment(ArraySegment<byte> binaryData, bool asText)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    await Send(binaryData, asText, CancellationTokenSource.Token).ConfigureAwait(false);
                    TotalSentBytesCount += binaryData.Count;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                    {
                        var logEvent = new WebSocketEvent
                        (
                            direction: MessageType.WebSocketOutput,
                            webSocket: this,
                            content: Encoding.UTF8.GetString(binaryData),
                            length: binaryData.Count
                        );
                        await Admin.Console.Log(logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        private async Task _SendStream(Stream binaryData, bool asText)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed:
                    throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                {
                    var sentBytes = await Send(binaryData, asText, CancellationTokenSource.Token).ConfigureAwait(false);
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
                        await Admin.Console.Log(logEvent).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        /// <inheritdoc />
        public Task SendText(string data) => _SendString(data);

        /// <inheritdoc />
        public Task SendText(ArraySegment<byte> data) => _SendArraySegment(data, true);

        /// <inheritdoc />
        public Task SendText(Stream stream) => _SendStream(stream, true);

        /// <inheritdoc />
        public Task SendBinary(ArraySegment<byte> data) => _SendArraySegment(data, false);

        /// <inheritdoc />
        public Task SendBinary(Stream stream) => _SendStream(stream, false);

        /// <inheritdoc />
        public async Task SendTextRaw(string textData)
        {
            if (Status != WebSocketStatus.Open) return;
            await Send(textData, CancellationTokenSource.Token).ConfigureAwait(false);
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

        private async Task SendResultInfo(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false)
        {
            var info = result.Headers.Info;
            var errorInfo = result.Headers.Error;
            var timeInfo = "";
            if (timeElapsed != null)
                timeInfo = $" ({timeElapsed.Value.TotalMilliseconds} ms)";
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $" (see {errorInfo})";
            await _SendString($"{result.StatusCode.ToCode()}: {result.StatusDescription}{timeInfo}{tail}")
                .ConfigureAwait(false);
            if (writeHeaders)
                await SendJson(result.Headers, true).ConfigureAwait(false);
        }

        public async Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false)
        {
            if (!PreCheck(result)) return;
            await SendResultInfo(result, timeElapsed, writeHeaders).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? timeElapsed = null,
            bool writeHeaders = false, bool disposeResult = true)
        {
            try
            {
                await SendResult(serializedResult.Result, timeElapsed, writeHeaders).ConfigureAwait(false);
                await SendBinary(serializedResult.Body).ConfigureAwait(false);
            }
            finally
            {
                if (disposeResult)
                    await serializedResult.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task SendException(Exception exception)
        {
            var error = exception.AsError();
            error.SetContext(Context);
            await SendResult(error).ConfigureAwait(false);
        }

        private IJsonProvider JsonProvider { get; }
        private WebSocketController WebSocketController { get; }

        /// <inheritdoc />
        public async Task SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            await using var message = await GetMessageStream(false).ConfigureAwait(false);
            JsonProvider.SerializeToStream(message, item, prettyPrint, ignoreNulls);
        }

        #endregion

        #region Streaming

        internal async Task HandleStreamingTextInput(string textInput)
        {
            var (command, arg) = textInput.TSplit(' ');
            switch (command.ToUpperInvariant())
            {
                case "OPTIONS":
                case "MANIFEST":
                    await SendManifest().ConfigureAwait(false);
                    break;
                case "GET":
                    await StreamMessage(-1).ConfigureAwait(false);
                    break;
                case "NEXT" when int.TryParse(arg, out var nr) && nr > 0:
                    await StreamMessage(nr).ConfigureAwait(false);
                    break;
                case "NEXT":
                    await StreamMessage(1).ConfigureAwait(false);
                    break;
                case "CLOSE":
                    await CloseStream().ConfigureAwait(false);
                    break;
            }
        }

        internal bool IsStreaming => StreamManifest != null;
        private StreamManifest StreamManifest { get; set; }

        private const int MaxStreamBufferSize = 16_000_000;
        private const int MinStreamBufferSize = 512;

        /// <inheritdoc />
        public async Task StreamSerializedResult(ISerializedResult serializedResult, int messageSize,
            TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true)
        {
            if (serializedResult == null) throw new ArgumentNullException(nameof(serializedResult));
            var content = serializedResult.Result as Content;

            if (content == null || !(serializedResult.Body?.Length > 0))
            {
                await SendSerializedResult(serializedResult, serializedResult.TimeElapsed, writeHeaders, disposeResult)
                    .ConfigureAwait(false);
                return;
            }
            if (content.IsLocked)
                throw new InvalidOperationException(
                    "Unable to stream a result that is already assigned to a different streaming " +
                    "job. A result can only be streamed once.");
            content.IsLocked = true;
            if (TerminalConnection != null)
                await TerminalConnection.Suspend();
            if (messageSize < MinStreamBufferSize)
                messageSize = MinStreamBufferSize;
            else if (MaxStreamBufferSize < messageSize)
                messageSize = MaxStreamBufferSize;
            if (StreamManifest != null)
                await StreamManifest.DisposeAsync().ConfigureAwait(false);
            StreamManifest = new StreamManifest(serializedResult, messageSize);
            await SendJson(StreamManifest).ConfigureAwait(false);
            buffer = null;
        }

        private byte[] buffer;

        private async Task CloseStream()
        {
            await SendText($"499: Client closed request. Streamed {StreamManifest.CurrentMessageIndex} " +
                           $"of {StreamManifest.NrOfMessages} messages.").ConfigureAwait(false);
            StopStreaming();
        }

        private void StopStreaming()
        {
            StreamManifest.Dispose();
            StreamManifest = null;
            buffer = null;
            TerminalConnection?.Unsuspend();
        }

        private async Task SendManifest() => await SendJson(StreamManifest).ConfigureAwait(false);

        private async Task StreamMessage(int nr)
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
                    var read = StreamManifest.Result.Body.ReadAsync(buffer, 0, buffer.Length);
                    var message = StreamManifest.Messages[StreamManifest.CurrentMessageIndex];
                    await read.ConfigureAwait(false);
                    await SendBinary(new ArraySegment<byte>(buffer, 0, (int) message.Length)).ConfigureAwait(false);
                    message.IsSent = true;
                    StreamManifest.MessagesStreamed += 1;
                    StreamManifest.MessagesRemaining -= 1;
                    StreamManifest.BytesStreamed += message.Length;
                    StreamManifest.BytesRemaining -= message.Length;
                    StreamManifest.CurrentMessageIndex += 1;
                }
                if (StreamManifest.Messages[StreamManifest.LastIndex].IsSent)
                {
                    await SendText($"200: OK. {StreamManifest.NrOfMessages} messages sucessfully streamed.")
                        .ConfigureAwait(false);
                    StopStreaming();
                }
            }
            catch (Exception e)
            {
                await SendException(e).ConfigureAwait(false);
                await SendText(
                    $"500: Error during streaming. Streamed {StreamManifest?.CurrentMessageIndex ?? 0} " +
                    $"of {StreamManifest?.NrOfMessages ?? 1} messages.").ConfigureAwait(false);
                StopStreaming();
            }
        }

        #endregion

        #endregion

        /// <inheritdoc />
        public override string ToString() => Id;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is WebSocket ws && ws.Id == Id;

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();

        protected WebSocket(string webSocketId, RESTableContext context, Client client)
        {
            Id = webSocketId;
            Status = WebSocketStatus.Waiting;
            Client = client;
            Context = context;
            CancellationTokenSource = new CancellationTokenSource();
            OngoingTasks = new HashSet<Task>();
            JsonProvider = context.Services.GetService<IJsonProvider>();
            WebSocketController = context.Services.GetService<WebSocketController>();
        }
    }
}