using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTable.ContentTypeProviders;
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
    public abstract class WebSocket : IWebSocket, IWebSocketInternal, ITraceable, IAsyncDisposable
    {
        static WebSocket() => BinaryCache = new BinaryCache();
        private static BinaryCache BinaryCache { get; }

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
        public void SetStatus(WebSocketStatus status) => Status = status;

        /// <inheritdoc />
        /// <summary>
        /// The headers contained in the WebSocket upgrade request
        /// </summary>
        public Headers Headers { get; internal set; }

        /// <inheritdoc />
        /// <summary>
        /// The cookies contained in the WebSocket upgrade request
        /// </summary>
        public ReadonlyCookies Cookies => Client.Cookies.AsReadonly();

        /// <inheritdoc />
        public string TraceId => Id;

        /// <inheritdoc />
        /// <summary>
        /// The context in which this WebSocket was opened
        /// </summary>
        public RESTableContext Context { get; internal set; }

        private ulong BytesReceived { get; set; }
        private ulong BytesSent { get; set; }

        internal ITerminalResource TerminalResource => TerminalConnection?.Resource;
        internal ITerminal Terminal => TerminalConnection?.Terminal;

        private WebSocketConnection TerminalConnection { get; set; }

        internal AppProfile GetAppProfile() => new(this);

        public Task LifetimeTask { get; private set; }

        internal void ConnectTo(ITerminal terminal, ITerminalResource resource)
        {
            ReleaseTerminal();
            TerminalConnection = new WebSocketConnection(this, terminal, resource);
        }

        private void ReleaseTerminal()
        {
            TerminalConnection?.DisposeAsync().AsTask().Wait();
            TerminalConnection = null;
        }

        internal void SetContext(IRequest upgradeRequest)
        {
            Context = new WebSocketContext(this, Client);
            Headers = upgradeRequest.Headers;
        }

        internal async Task Open()
        {
            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    await SendUpgrade();
                    LifetimeTask = InitLifetimeTask();
                    Status = WebSocketStatus.Open;
                    OpenedAt = DateTime.Now;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                        await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketOpen, this));
                    break;
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        private bool disposed;

        /// <inheritdoc />
        /// <summary>
        /// Disposes the WebSocket. Same as Disconnect()
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            WebSocketController.RemoveWebSocket(Id);
            Status = WebSocketStatus.PendingClose;
            StreamManifest?.Dispose();
            StreamManifest = null;
            var terminalName = TerminalConnection?.Resource?.Name;
            ReleaseTerminal();
            if (IsConnected)
                await Close();
            Status = WebSocketStatus.Closed;
            ClosedAt = DateTime.Now;
            if (terminalName != Admin.Console.TypeName)
                await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketClose, this));
            disposed = true;
        }

        /// <summary>
        /// Sends text data to the client over the WebSocket
        /// </summary>
        protected abstract Task Send(string text);

        /// <summary>
        /// Sends binary or text data to the client over the WebSocket
        /// </summary>
        protected abstract Task Send(byte[] data, bool asText, int offset, int length);
        
        /// <summary>
        /// Is the WebSocket currently connected?
        /// </summary>
        protected abstract bool IsConnected { get; }

        /// <summary>
        /// Sends the WebSocket upgrade and initiates the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task SendUpgrade();

        /// <summary>
        /// A task that represents the lifetime of the WebSocket, handling incoming messages and
        /// sending responses.
        /// </summary>
        /// <returns></returns>
        protected abstract Task InitLifetimeTask();

        /// <summary>
        /// Disconnects the actual underlying WebSocket connection
        /// </summary>
        protected abstract Task Close();

        #region IWebSocket

        /// <inheritdoc />
        public void DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => DirectTo(Shell.TerminalResource);

        /// <inheritdoc />
        public void DirectTo<T>(ITerminalResource<T> resource, ICollection<Condition<T>> assignments = null) where T : class, ITerminal
        {
            if (Status != WebSocketStatus.Open)
                throw new InvalidOperationException($"Unable to send WebSocket with status '{Status}' to terminal '{resource.Name}'");
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(assignments);
            Context.WebSocket.ConnectTo(newTerminal, _resource);
            newTerminal.Open();
        }

        #region Input

        internal async Task HandleTextInput(string textData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsTextInput)
            {
                await SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support text input"));
                return;
            }
            if (TerminalConnection.Resource?.Name != Admin.Console.TypeName)
                await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketInput, this, textData, (ulong) Encoding.UTF8.GetByteCount(textData)));
            await TerminalConnection.Terminal.HandleTextInput(textData);
            BytesReceived += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        internal async Task HandleBinaryInput(byte[] binaryData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsBinaryInput)
            {
                await SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support binary input"));
                return;
            }
            if (TerminalConnection.Resource?.Name != Admin.Console.TypeName)
                await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketInput, this, Encoding.UTF8.GetString(binaryData), (ulong) binaryData.LongLength));
            await TerminalConnection.Terminal.HandleBinaryInput(binaryData);
            BytesSent += (ulong) binaryData.Length;
        }

        #endregion

        #region Simple output

        private async Task _SendText(string textData)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    await Send(textData);
                    BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                        await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketOutput, this, textData, (ulong) Encoding.UTF8.GetByteCount(textData)));
                    break;
            }
        }

        private async Task _SendBinary(byte[] binaryData, bool isText, int offset, int length)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    await Send(binaryData, isText, offset, length);
                    BytesSent += (ulong) length;
                    if (TerminalConnection?.Resource.Name != Admin.Console.TypeName)
                        await Admin.Console.Log(new WebSocketEvent(MessageType.WebSocketOutput, this, Encoding.UTF8.GetString(binaryData), (ulong) binaryData.LongLength));
                    break;
            }
        }

        /// <inheritdoc />
        public Task SendText(string data) => _SendText(data);

        /// <inheritdoc />
        public Task SendText(byte[] data, int offset, int length) => _SendBinary(data, true, offset, length);

        /// <inheritdoc />
        public Task SendBinary(byte[] data, int offset, int length) => _SendBinary(data, false, offset, length);

        /// <inheritdoc />
        public async Task SendTextRaw(string textData)
        {
            if (Status != WebSocketStatus.Open) return;
            await Send(textData);
            BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        /// <inheritdoc />
        public async Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true)
        {
            try
            {
                switch (Status)
                {
                    case WebSocketStatus.Open: break;
                    case var other: throw new InvalidOperationException($"Unable to send results to a WebSocket with status '{other}'");
                }
                if (result is WebSocketUpgradeSuccessful) return;
                var serialized = result.IsSerialized ? (ISerializedResult) result : result.Serialize();

                if (serialized is Content {IsLocked: true})
                    throw new InvalidOperationException("Unable to send a result that is already assigned to a Websocket streaming " +
                                                        "job. Streaming results are locked, and can only be streamed once.");

                var foundCached = BinaryCache.TryGet(serialized, out var body);
                if (!foundCached && serialized.Body?.CanRead == false)
                    throw new InvalidOperationException($"Unable to send a disposed result over Websocket '{Id}'. To send the " +
                                                        "same result multiple times, set 'disposeResult' to false in the call to " +
                                                        "'SendResult()'.");

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
                await _SendText($"{result.StatusCode.ToCode()}: {result.StatusDescription}{timeInfo}{tail}");
                if (writeHeaders)
                    await SendJson(result.Headers, true);
                if (body == null && serialized.Body != null && (!serialized.Body.CanSeek || serialized.Body.Length > 0))
                    body = serialized.Body.ToByteArray();
                if (body != null)
                {
                    if (!foundCached)
                        BinaryCache.Cache(serialized, body);
                    await SendBinary(body, 0, body.Length);
                }
            }
            finally
            {
                if (disposeResult)
                    await result.DisposeAsync();
            }
        }

        /// <inheritdoc />
        public async Task SendException(Exception exception)
        {
            var error = exception.AsError();
            error.SetTrace(this);
            await SendResult(error);
        }

        /// <inheritdoc />
        public async Task SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!BinaryCache.TryGet(item, out var body))
            {
                Formatting _prettyPrint;
                if (prettyPrint == null)
                    _prettyPrint = Admin.Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                else _prettyPrint = prettyPrint.Value ? Formatting.Indented : Formatting.None;
                var stream = Providers.Json.SerializeStream(item, _prettyPrint, ignoreNulls);
                body = await stream.ToByteArrayAsync();
                BinaryCache.Cache(item, body);
            }
            await _SendBinary(body, asText, 0, body.Length);
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
                    await SendManifest();
                    break;
                case "GET":
                    await StreamMessage(-1);
                    break;
                case "NEXT" when int.TryParse(arg, out var nr) && nr > 0:
                    await StreamMessage(nr);
                    break;
                case "NEXT":
                    await StreamMessage(1);
                    break;
                case "CLOSE":
                    await CloseStream();
                    break;
            }
        }

        internal bool IsStreaming => StreamManifest != null;
        private StreamManifest StreamManifest { get; set; }

        private const int MaxStreamBufferSize = 16_000_000;
        private const int MinStreamBufferSize = 512;

        /// <inheritdoc />
        public async Task StreamResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!result.IsSerialized)
                result = result.Serialize();
            if (!(result is Content content) || !(content.Body?.Length > 0))
            {
                await SendResult(result, result.TimeElapsed, writeHeaders, disposeResult);
                return;
            }
            if (content.IsLocked)
                throw new InvalidOperationException("Unable to stream a result that is already assigned to a different streaming " +
                                                    "job. A result can only be streamed once.");
            content.IsLocked = true;
            TerminalConnection?.Suspend();
            if (messageSize < MinStreamBufferSize)
                messageSize = MinStreamBufferSize;
            else if (MaxStreamBufferSize < messageSize)
                messageSize = MaxStreamBufferSize;
            StreamManifest?.Dispose();
            StreamManifest = new StreamManifest(content, messageSize);
            await SendJson(StreamManifest);
            buffer = null;
        }

        private byte[] buffer;

        private async Task CloseStream()
        {
            await SendText($"499: Client closed request. Streamed {StreamManifest.CurrentMessageIndex} " +
                           $"of {StreamManifest.NrOfMessages} messages.");
            StopStreaming();
        }

        private void StopStreaming()
        {
            StreamManifest.Dispose();
            StreamManifest = null;
            buffer = null;
            TerminalConnection?.Unsuspend();
        }

        private async Task SendManifest() => await SendJson(StreamManifest);

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
                    var read = StreamManifest.Content.Body.ReadAsync(buffer, 0, buffer.Length);
                    var message = StreamManifest.Messages[StreamManifest.CurrentMessageIndex];
                    await read;
                    await SendBinary(buffer, 0, (int) message.Length);
                    message.IsSent = true;
                    StreamManifest.MessagesStreamed += 1;
                    StreamManifest.MessagesRemaining -= 1;
                    StreamManifest.BytesStreamed += message.Length;
                    StreamManifest.BytesRemaining -= message.Length;
                    StreamManifest.CurrentMessageIndex += 1;
                }
                if (StreamManifest.Messages[StreamManifest.LastIndex].IsSent)
                {
                    await SendText($"200: OK. {StreamManifest.NrOfMessages} messages sucessfully streamed.");
                    StopStreaming();
                }
            }
            catch (Exception e)
            {
                await SendException(e);
                await SendText(
                    $"500: Error during streaming. Streamed {StreamManifest?.CurrentMessageIndex ?? 0} " +
                    $"of {StreamManifest?.NrOfMessages ?? 1} messages.");
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

        protected WebSocket(string webSocketId, Client client)
        {
            Id = webSocketId;
            Status = WebSocketStatus.Waiting;
            Client = client;
        }
    }
}