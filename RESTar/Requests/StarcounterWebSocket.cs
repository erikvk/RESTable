using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Success;
using RESTar.Serialization;
using RESTar.WebSockets;
using Starcounter;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;
using static RESTar.Serialization.Serializer;
using Console = RESTar.Admin.Console;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : IWebSocket, IWebSocketInternal
    {
        private ConcurrentQueue<(object data, bool isText)> Queue;
        private WebSocket WebSocket;
        private readonly Request ScRequest;
        private readonly string GroupName;
        private bool AbortOpen;
        public string Id { get; }
        public ITarget Target { get; set; }
        public ITerminal Terminal { get; set; }
        public TerminalResource TerminalResource { get; set; }
        public DateTime Opened { get; private set; }
        public DateTime Closed { get; private set; }
        public ulong BytesReceived { get; internal set; }
        public ulong BytesSent { get; private set; }
        public TCPConnection TcpConnection { get; }
        public Headers Headers { get; }

        public void HandleTextInput(string textData)
        {
            if (!Terminal.SupportsTextInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalResource.FullName}' does not support text input"));
                return;
            }
            Console.LogWebSocketTextInput(textData, this);
            Terminal.HandleTextInput(textData);
            BytesReceived += (ulong) Encoding.UTF8.GetByteCount(textData);
        }

        public void HandleBinaryInput(byte[] binaryData)
        {
            if (!Terminal.SupportsBinaryInput)
            {
                SendResult(new UnsupportedWebSocketInput($"Terminal '{TerminalResource.FullName}' does not support binary input"));
                return;
            }
            Console.LogWebSocketBinaryInput(binaryData.Length, this);
            Terminal.HandleBinaryInput(binaryData);
            BytesSent += (ulong) binaryData.Length;
        }

        public void Dispose()
        {
            Status = WebSocketStatus.PendingClose;
            Terminal.Dispose();
            Status = WebSocketStatus.Closed;
            Closed = DateTime.Now;
            Console.LogWebSocketClosed(this);
            WebSocketController.HandleDisconnect(Id);
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
                    Console.LogWebSocketTextOutput(textData, this);
                    break;
                case WebSocketStatus.PendingOpen:
                    Enqueue(textData, true);
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
                    Console.LogWebSocketTextOutput($"[{binaryData.Length} bytes]", this);
                    break;
                case WebSocketStatus.PendingOpen:
                    Enqueue(binaryData, isText);
                    break;
            }
        }

        public void SendResult(IFinalizedResult result)
        {
            switch (result)
            {
                case Report _:
                case Entities _:
                    SendText(result.Body);
                    break;
                default:
                    var info = result.Headers["RESTar-Info"];
                    var errorInfo = result.Headers["ErrorInfo"];
                    var tail = "";
                    if (info != null)
                        tail += $". {info}";
                    if (errorInfo != null)
                        tail += $". See {errorInfo}";
                    _SendText($"{result.StatusCode.ToCode()}: {result.StatusDescription}{tail}");
                    break;
            }
        }

        public void SendText(string data) => _SendText(data);
        public void SendText(byte[] data) => _SendBinary(data, true);
        public void SendText(Stream data) => _SendBinary(data.ToByteArray(), true);
        public void SendBinary(byte[] data) => _SendBinary(data, false);
        public void SendBinary(Stream data) => _SendBinary(data.ToByteArray(), false);

        public void SendJson(object item)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = _PrettyPrint ? Indented : None;
                Serializer.Json.Serialize(jwr, item);
            }
            stream.Seek(0, SeekOrigin.Begin);
            _SendBinary(stream.ToArray(), true);
        }

        private void Enqueue(object data, bool isText)
        {
            Queue = Queue ?? new ConcurrentQueue<(object, bool)>();
            Queue.Enqueue((data, isText));
        }

        public WebSocketStatus Status { get; private set; }

        public void Disconnect()
        {
            switch (Status)
            {
                case WebSocketStatus.Open:
                    WebSocket.Disconnect();
                    break;
                case WebSocketStatus.PendingOpen:
                    AbortOpen = true;
                    break;
            }
        }

        public override string ToString() => Id;
        public override bool Equals(object obj) => obj is StarcounterWebSocket sws && sws.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();

        public void Open()
        {
            WebSocket = ScRequest.SendUpgrade(GroupName);
            Status = WebSocketStatus.Open;
            Opened = DateTime.Now;
            Console.LogWebSocketOpen(this);
            Queue?.ForEach(message =>
            {
                switch (message.data)
                {
                    case string textData:
                        _SendText(textData);
                        break;
                    case byte[] byteData:
                        _SendBinary(byteData, message.isText);
                        break;
                }
            });
            Queue = null;
            if (AbortOpen) Disconnect();
        }

        internal StarcounterWebSocket(string groupName, Request scRequest, Headers headers, TCPConnection tcpConnection)
        {
            GroupName = groupName;
            ScRequest = scRequest;
            Id = scRequest.GetWebSocketId().ToString();
            Headers = headers;
            TcpConnection = tcpConnection;
            Status = WebSocketStatus.PendingOpen;
        }
    }
}