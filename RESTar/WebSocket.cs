using System;
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
        public string Id { get; }
        public DateTime Opened { get; private set; }
        public DateTime Closed { get; private set; }
        public Client Client { get; }
        public WebSocketStatus Status { get; private set; }
        public Headers Headers { get; internal set; }
        public Context Context { get; private set; }
        public string TraceId => Id;

        private ulong BytesReceived { get; set; }
        private ulong BytesSent { get; set; }

        internal WebSocketConnection TerminalConnection { get; set; }

        internal ConnectionProfile GetConnectionProfile() => new ConnectionProfile(this);

        internal void ReleaseTerminal()
        {
            TerminalConnection?.Dispose();
            TerminalConnection = null;
        }

        internal void Open(IRequest upgradeRequest)
        {
            Context = new WebSocketContext(Client);
            Headers = upgradeRequest.Headers;
            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    SendUpgrade();
                    Status = WebSocketStatus.Open;
                    Opened = DateTime.Now;
                    if (TerminalConnection?.TerminalResource.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(LogEventType.WebSocketOpen, this));
                    break;
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        public void Disconnect() => Dispose();

        private bool disposed;

        public void Dispose()
        {
            Client.Dispose();
            if (disposed) return;
            Status = WebSocketStatus.PendingClose;
            var terminalName = TerminalConnection?.TerminalResource?.Name;
            ReleaseTerminal();
            if (IsConnected) Disconnect();
            Status = WebSocketStatus.Closed;
            Closed = DateTime.Now;
            if (terminalName != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketClose, this));
            disposed = true;
        }

        protected abstract void Send(string text);
        protected abstract void Send(byte[] data, bool isText);
        protected abstract bool IsConnected { get; }
        protected abstract void SendUpgrade();
        protected abstract void DisconnectWebSocket();

        #region IWebSocket

        public void SendToShell() => Shell.TerminalResource.InstantiateFor(this);

        public void SendTo(ITerminalResource terminalResource)
        {
            if (terminalResource == null)
                throw new ArgumentNullException(nameof(terminalResource));
            var _terminalResource = (ITerminalResourceInternal) terminalResource;
            _terminalResource.InstantiateFor(this);
        }

        public void HandleTextInput(string textData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsTextInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.TerminalResource.Name}' does not support text input"));
                return;
            }
            if (TerminalConnection.TerminalResource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketInput, this, textData, Encoding.UTF8.GetByteCount(textData)));
            TerminalConnection.Terminal.HandleTextInput(textData);
            BytesReceived += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        public void HandleBinaryInput(byte[] binaryData)
        {
            if (TerminalConnection == null) return;
            if (!TerminalConnection.Terminal.SupportsBinaryInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalConnection.TerminalResource.Name}' does not support binary input"));
                return;
            }
            if (TerminalConnection.TerminalResource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(LogEventType.WebSocketInput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
            TerminalConnection.Terminal.HandleBinaryInput(binaryData);
            BytesSent += (ulong) binaryData.Length;
        }

        public void SendTextRaw(string textData)
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
                    if (TerminalConnection?.TerminalResource.Name != Console.TypeName)
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
                    if (TerminalConnection?.TerminalResource.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(LogEventType.WebSocketOutput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
                    break;
            }
        }

        public void SendResult(IFinalizedResult result, bool includeStatusWithContent = true, TimeSpan? timeElapsed = null)
        {
            switch (Status)
            {
                case WebSocketStatus.Open: break;
                case var closed:
                    throw new InvalidOperationException($"Unable to send results to a WebSocket with status '{closed}'");
            }
            if (result is WebSocketResult) return;

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

        public void SendException(Exception exception) => SendResult(RESTarError.GetError(exception));
        public void SendText(string data) => _SendText(data);
        public void SendText(byte[] data) => _SendBinary(data, true);
        public void SendText(Stream data) => _SendBinary(data.ToByteArray(), true);
        public void SendBinary(byte[] data) => _SendBinary(data, false);
        public void SendBinary(Stream data) => _SendBinary(data.ToByteArray(), false);

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

        public override string ToString() => Id;
        public override bool Equals(object obj) => obj is StarcounterWebSocket sws && sws.Id == Id;
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