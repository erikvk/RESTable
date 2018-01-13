using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
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
        private ConcurrentQueue<byte[]> Queue;
        private WebSocket WebSocket;
        private readonly Request Request;
        private readonly string GroupName;
        private bool AbortOpen;
        public string Id { get; }
        public WebSocketReceiveAction InputHandler { get; set; }
        public WebSocketDisconnectAction DisconnectHandler { get; set; }
        public void HandleInput(string input) => InputHandler?.Invoke(this, input);
        public string CurrentLocation { get; set; }
        public void SetCurrentLocation(string location) => CurrentLocation = location;
        public IPAddress ClientIpAddress { get; }
        public ITarget Target { get; set; }
        public bool IsShell { get; private set; }
        public void SetQueryProperties(string query, Headers headers, TCPConnection connection)
        {
            throw new NotImplementedException();
        }

        public void SetShellHandler(WebSocketReceiveAction shellHandler)
        {
            if (InputHandler != null) return;
            InputHandler = shellHandler;
            IsShell = true;
        }

        public void HandleDisconnect()
        {
            Status = WebSocketStatus.PendingClose;
            DisconnectHandler?.Invoke(this);
            Status = WebSocketStatus.Closed;
            _WebSocketController.HandleDisconnect(Id);
        }

        public WebSocketStatus Status { get; private set; }

        public void Send(string data)
        {
            Send(data.ToBytes());
            Admin.Console.LogWebSocketOutput(this, data);
        }

        public void SendEntities<T>(IEnumerable<T> items) where T : class
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = _PrettyPrint ? Indented : None;
                Serializer.Json.Serialize(jwr, items);
            }
            stream.Seek(0, SeekOrigin.Begin);
            Send(stream);
        }

        public void Send(Stream data)
        {
            var bytes = data.ToByteArray();
            Send(bytes);
            Admin.Console.LogWebSocketOutput(this, data.GetString());
        }

        private void Send(byte[] bytes)
        {
            switch (Status)
            {
                case WebSocketStatus.Closed: throw new InvalidOperationException("Cannot send data to a closed WebSocket");
                case WebSocketStatus.Open:
                    WebSocket.Send(bytes, true);
                    break;
                case WebSocketStatus.PendingOpen:
                    Enqueue(bytes);
                    break;
            }
        }

        private void Enqueue(byte[] data)
        {
            Queue = Queue ?? new ConcurrentQueue<byte[]>();
            Queue.Enqueue(data);
        }

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
            Queue?.ForEach(message => WebSocket.Send(message));
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