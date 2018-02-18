using System;
using System.IO;
using System.Text;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Success;
using RESTar.Serialization;
using RESTar.Serialization.NativeProtocol;
using RESTar.WebSockets;
using Starcounter;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;
using static RESTar.Logging.LogEventType;
using static RESTar.Serialization.Serializer;
using Console = RESTar.Admin.Console;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : IWebSocket, IWebSocketInternal
    {
        private WebSocket WebSocket;
        private readonly Request ScRequest;
        private readonly string GroupName;
        public string TraceId { get; }

        private ITerminal terminal;

        public ITerminal Terminal
        {
            get => terminal;
            set
            {
                terminal?.Dispose();
                terminal = value;
            }
        }

        public ITerminalResource TerminalResource { get; set; }
        public DateTime Opened { get; private set; }
        public DateTime Closed { get; private set; }
        public ulong BytesReceived { get; internal set; }
        public ulong BytesSent { get; private set; }
        public TCPConnection TcpConnection { get; }
        public Headers Headers { get; }
        public WebSocketStatus Status { get; private set; }
        public ConnectionProfile GetConnectionProfile() => new ConnectionProfile(this);
        public void SendToShell() => Shell.TerminalResource.InstantiateFor(this);

        public void SendTo(ITerminalResource terminalResource)
        {
            if (terminalResource == null)
                throw new ArgumentNullException(nameof(terminalResource));
            var _terminalResource = (ITerminalResourceInternal) terminalResource;
            _terminalResource.InstantiateFor(this);
        }

        #region Interface

        public void HandleTextInput(string textData)
        {
            if (Terminal == null) return;
            if (!Terminal.SupportsTextInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalResource.Name}' does not support text input"));
                return;
            }
            if (TerminalResource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(WebSocketInput, this, textData, Encoding.UTF8.GetByteCount(textData)));
            Terminal.HandleTextInput(textData);
            BytesReceived += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        public void HandleBinaryInput(byte[] binaryData)
        {
            if (Terminal == null) return;
            if (!Terminal.SupportsBinaryInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalResource.Name}' does not support binary input"));
                return;
            }
            if (TerminalResource?.Name != Console.TypeName)
                Console.Log(new WebSocketEvent(WebSocketInput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
            Terminal.HandleBinaryInput(binaryData);
            BytesSent += (ulong) binaryData.Length;
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            var terminalName = TerminalResource?.Name;
            if (!WebSocket.IsDead())
                WebSocket.Disconnect();
            if (Status == WebSocketStatus.Closed)
            {
                Terminal = null;
                TerminalResource = null;
                return;
            }
            Status = WebSocketStatus.PendingClose;
            Terminal = null;
            TerminalResource = null;
            Status = WebSocketStatus.Closed;
            Closed = DateTime.Now;
            if (terminalName != Console.TypeName)
                Console.Log(new WebSocketEvent(WebSocketClose, this));
            disposed = true;
        }

        public void SendTextRaw(string textData)
        {
            if (Status != WebSocketStatus.Open) return;
            WebSocket.Send(textData);
            BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        private void _SendText(string textData)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    WebSocket.Send(textData);
                    BytesSent += (ulong) Encoding.UTF8.GetByteCount(textData);
                    if (TerminalResource?.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(WebSocketOutput, this, textData, Encoding.UTF8.GetByteCount(textData)));
                    break;
            }
        }

        private void _SendBinary(byte[] binaryData, bool isText)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    WebSocket.Send(binaryData, isText);
                    BytesSent += (ulong) binaryData.Length;
                    if (TerminalResource?.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(WebSocketOutput, this, Encoding.UTF8.GetString(binaryData), binaryData.Length));
                    break;
            }
        }

        public void SendResult(IFinalizedResult result, bool includeStatusWithContent = true)
        {
            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    Open();
                    break;
                case WebSocketStatus.Open: break;
                case var closed:
                    throw new InvalidOperationException($"Unable to send results to a WebSocket with status '{closed}'");
            }
            if (result is WebSocketResult) return;

            void sendStatus()
            {
                var info = result.Headers["RESTar-Info"];
                var errorInfo = result.Headers["ErrorInfo"];
                var tail = "";
                if (info != null)
                    tail += $". {info}";
                if (errorInfo != null)
                    tail += $" (see {errorInfo})";
                _SendText($"{result.StatusCode.ToCode()}: {result.StatusDescription}{tail}");
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

        public void SendJson(object item, bool? prettyPrint = null)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                var _prettyPrint = prettyPrint ?? _PrettyPrint;
                Serializer.Json.Formatting = _prettyPrint ? Indented : None;
                Serializer.Json.Serialize(jwr, item);
            }
            stream.Seek(0, SeekOrigin.Begin);
            _SendBinary(stream.ToArray(), true);
        }

        #endregion

        public void Disconnect() => Dispose();

        public override string ToString() => TraceId;
        public override bool Equals(object obj) => obj is StarcounterWebSocket sws && sws.TraceId == TraceId;
        public override int GetHashCode() => TraceId.GetHashCode();

        public void Open()
        {
            switch (Status)
            {
                case WebSocketStatus.Waiting:
                    WebSocket = ScRequest.SendUpgrade(GroupName);
                    Status = WebSocketStatus.Open;
                    Opened = DateTime.Now;
                    if (TerminalResource?.Name != Console.TypeName)
                        Console.Log(new WebSocketEvent(WebSocketOpen, this));
                    break;
                default: throw new InvalidOperationException($"Unable to open WebSocket with status '{Status}'");
            }
        }

        internal StarcounterWebSocket(string groupName, Request scRequest, Headers headers, TCPConnection tcpConnection)
        {
            GroupName = groupName;
            ScRequest = scRequest;
            TraceId = DbHelper.Base64EncodeObjectNo(scRequest.GetWebSocketId());
            Headers = headers;
            TcpConnection = tcpConnection;
            Status = WebSocketStatus.Waiting;
        }
    }
}