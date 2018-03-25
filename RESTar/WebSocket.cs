using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Success;
using RESTar.Serialization;
using RESTar.Starcounter;
using RESTar.WebSockets;
using Console = RESTar.Admin.Console;

namespace RESTar
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="ITraceable" />
    /// <summary>
    /// </summary>
    public abstract class WebSocket : IWebSocket, ITraceable, IDisposable
    {
        /// <summary>
        /// The ID of the WebSocket
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The date and time when this WebSocket was opened
        /// </summary>
        public DateTime Opened { get; private set; }

        /// <summary>
        /// The date and time when this WebSocket was closed
        /// </summary>
        public DateTime Closed { get; private set; }

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
        /// <summary>
        /// The headers contained in the WebSocket upgrade request
        /// </summary>
        public Headers Headers { get; internal set; }

        /// <inheritdoc />
        public string TraceId => Id;

        /// <inheritdoc />
        /// <summary>
        /// The context in which this WebSocket was opened
        /// </summary>
        public Context Context { get; private set; }

        private ulong BytesReceived { get; set; }
        private ulong BytesSent { get; set; }

        internal ITerminalResource TerminalResource => TerminalConnection?.Resource;
        internal ITerminal Terminal => TerminalConnection?.Terminal;

        private WebSocketConnection TerminalConnection { get; set; }

        internal ConnectionProfile GetConnectionProfile() => new ConnectionProfile(this);

        internal void ConnectTo(ITerminal terminal, ITerminalResource resource)
        {
            ReleaseTerminal();
            TerminalConnection = new WebSocketConnection(this, terminal, resource);
        }

        internal void ReleaseTerminal()
        {
            TerminalConnection?.Dispose();
            TerminalConnection = null;
        }

        internal void SetContext(IRequest upgradeRequest)
        {
            Context = new WebSocketContext(this, Client);
            Headers = upgradeRequest.Headers;
        }

        internal void Open()
        {
            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    SendUpgrade();
                    Status = WebSocketStatus.Open;
                    Opened = DateTime.Now;
                    if (TerminalConnection?.Resource.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(LogEventType.WebSocketOpen, this));
                    break;
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        /// <summary>
        /// Disposes the WebSocket. Same as Dispose()
        /// </summary>
        public void Disconnect() => Dispose();

        private bool disposed;

        /// <inheritdoc />
        /// <summary>
        /// Disposes the WebSocket. Same as Disconnect()
        /// </summary>
        public void Dispose()
        {
            Client.Dispose();
            if (disposed) return;
            Status = WebSocketStatus.PendingClose;
            var terminalName = TerminalConnection?.Resource?.Name;
            ReleaseTerminal();
            if (IsConnected) DisconnectWebSocket();
            Status = WebSocketStatus.Closed;
            Closed = DateTime.Now;
            if (terminalName != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketClose, this));
            disposed = true;
        }

        /// <summary>
        /// Sends text data to the client over the WebSocket
        /// </summary>
        protected abstract void Send(string text);

        /// <summary>
        /// Sends binary or text data to the client over the WebSocket
        /// </summary>
        protected abstract void Send(byte[] data, bool isText);

        /// <summary>
        /// Is the WebSocket currently connected?
        /// </summary>
        protected abstract bool IsConnected { get; }

        /// <summary>
        /// Sends the WebSocket upgrade and initiates the actual underlying WebSocket connection
        /// </summary>
        protected abstract void SendUpgrade();

        /// <summary>
        /// Disconnects the actual underlying WebSocket connection
        /// </summary>
        protected abstract void DisconnectWebSocket();

        #region IWebSocket

        /// <inheritdoc />
        public void SendToShell(IEnumerable<Condition<Shell>> assignments = null) => SendTo(Shell.TerminalResource);

        /// <inheritdoc />
        public void SendTo<T>(ITerminalResource<T> resource, IEnumerable<Condition<T>> assignments = null) where T : class, ITerminal
        {
            if (Status != WebSocketStatus.Open)
                throw new InvalidOperationException($"Unable to send WebSocket with status '{Status}' to terminal '{resource.Name}'");
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            var _resource = (Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(assignments);
            Context.WebSocket.ConnectTo(newTerminal, _resource);
            newTerminal.Open();
        }

        internal void HandleTextInput(string textData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsTextInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support text input"));
                return;
            }
            if (TerminalConnection.Resource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketInput, this, textData, Encoding.UTF8.GetByteCount(textData)));
            TerminalConnection.Terminal.HandleTextInput(textData);
            BytesReceived += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        internal void HandleBinaryInput(byte[] binaryData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsBinaryInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.Resource.Name}' does not support binary input"));
                return;
            }
            if (TerminalConnection.Resource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketInput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
            TerminalConnection.Terminal.HandleBinaryInput(binaryData);
            BytesSent += (ulong) binaryData.Length;
        }

        internal void SendTextRaw(string textData)
        {
            if (Status != WebSocketStatus.Open) return;
            Send(textData);
            BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        private void _SendText(string textData)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    Send(textData);
                    BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
                    if (TerminalConnection?.Resource.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(LogEventType.WebSocketOutput, this, textData, Encoding.UTF8.GetByteCount(textData)));
                    break;
            }
        }

        private void _SendBinary(byte[] binaryData, bool isText)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    Send(binaryData, isText);
                    BytesSent += (ulong) binaryData.Length;
                    if (TerminalConnection?.Resource.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(LogEventType.WebSocketOutput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
                    break;
            }
        }

        /// <inheritdoc />
        public void SendResult(ISerializedResult result, bool includeStatusWithContent = true, TimeSpan? timeElapsed = null)
        {
            switch (Status)
            {
                case WebSocketStatus.Open: break;
                case var closed:
                    throw new InvalidOperationException($"Unable to send results to a WebSocket with status '{closed}'");
            }
            if (result is WebSocketUpgradeSuccessful) return;

            void sendStatus()
            {
                var info = result.Headers["RESTar-Info"];
                var errorInfo = result.Headers["ErrorInfo"];
                var timeInfo = "";
                if (timeElapsed != null)
                    timeInfo = $" ({timeElapsed.Value.TotalMilliseconds} ms)";
                var tail = "";
                if (info != null)
                    tail += $". {info}";
                if (errorInfo != null)
                    tail += $" (see {errorInfo})";
                _SendText($"{result.StatusCode.ToCode()}: {result.StatusDescription}{timeInfo}{tail}");
            }

            if (result.Body != null && (!result.Body.CanSeek || result.Body.Length > 0))
            {
                if (includeStatusWithContent)
                    sendStatus();
                SendText(result.Body);
            }
            else sendStatus();
        }

        /// <inheritdoc />
        public void SendException(Exception exception) => SendResult(RESTarError.GetError(exception));

        /// <inheritdoc />
        public void SendText(string data) => _SendText(data);

        /// <inheritdoc />
        public void SendText(byte[] data) => _SendBinary(data, true);

        /// <inheritdoc />
        public void SendText(Stream data) => _SendBinary(data.ToByteArray(), true);

        /// <inheritdoc />
        public void SendBinary(byte[] data) => _SendBinary(data, false);

        /// <inheritdoc />
        public void SendBinary(Stream data) => _SendBinary(data.ToByteArray(), false);

        /// <inheritdoc />
        public void SendJson(object item, bool? prettyPrint = null, bool ignoreNulls = false)
        {
            Formatting _prettyPrint;
            if (prettyPrint == null)
                _prettyPrint = Admin.Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
            else _prettyPrint = prettyPrint.Value ? Formatting.Indented : Formatting.None;
            var stream = Serializers.Json.SerializeStream(item, _prettyPrint, ignoreNulls);
            _SendBinary(stream.ToArray(), true);
        }

        #endregion

        /// <inheritdoc />
        public override string ToString() => Id;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ScWebSocket sws && sws.Id == Id;

        /// <inheritdoc />
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc />
        protected WebSocket(string webSocketId, Client client)
        {
            Id = webSocketId;
            Status = WebSocketStatus.Waiting;
            Client = client;
        }
    }
}