using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Serialization;
using RESTar.WebSockets;
using Starcounter;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;
using static RESTar.Serialization.Serializer;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : IWebSocket, IWebSocketInternal
    {
        private ConcurrentQueue<(object data, bool isText)> Queue;
        private WebSocket WebSocket;
        private readonly Request Request;
        private readonly string GroupName;
        private bool AbortOpen;
        public string Id { get; }
        public string CurrentLocation { get; set; }
        public void SetCurrentLocation(string location) => CurrentLocation = location;
        public IPAddress ClientIpAddress { get; }
        public ITarget Target { get; set; }
        public bool IsShell { get; private set; }
        public ITerminal Terminal { get; set; }

        private void _SendText(string textData)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    WebSocket.Send(textData);
                    //Admin.Console.LogWebSocketOutput(textData, this);
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
                    //Admin.Console.LogWebSocketOutput($"[{binaryData.Length} bytes]", this);
                    break;
                case WebSocketStatus.PendingOpen:
                    Enqueue(binaryData, isText);
                    break;
            }
        }

        public void SendText(string data) => _SendText(data);
        public void SendText(byte[] data) => _SendBinary(data, true);
        public void SendText(Stream data) => _SendBinary(data.ToByteArray(), true);
        public void SendBinary(byte[] data) => _SendBinary(data, false);
        public void SendBinary(Stream data) => _SendBinary(data.ToByteArray(), false);

        public void SendJson(object items)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = _PrettyPrint ? Indented : None;
                Serializer.Json.Serialize(jwr, items);
            }
            stream.Seek(0, SeekOrigin.Begin);
            _SendBinary(stream.ToArray(), true);
        }

        private void Enqueue(object data, bool isText)
        {
            Queue = Queue ?? new ConcurrentQueue<(object, bool)>();
            Queue.Enqueue((data, isText));
        }

        public void SetQueryProperties(string query, Headers headers, TCPConnection connection)
        {
            throw new NotImplementedException();
        }

        public void HandleDisconnect()
        {
            Status = WebSocketStatus.PendingClose;
            Status = WebSocketStatus.Closed;
            WebSocketController.HandleDisconnect(Id);
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
            WebSocket = Request.SendUpgrade(GroupName);
            Status = WebSocketStatus.Open;
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

        internal StarcounterWebSocket(string groupName, Request request, string initialLocation)
        {
            GroupName = groupName;
            Request = request;
            Id = request.GetWebSocketId().ToString();
            ClientIpAddress = request.ClientIpAddress;
            Status = WebSocketStatus.PendingOpen;
            CurrentLocation = initialLocation;
        }
    }
}