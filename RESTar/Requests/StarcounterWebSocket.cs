using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using Starcounter;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : IWebSocket, IWebSocketInternal
    {
        private ConcurrentQueue<byte[]> Queue;
        private WebSocket WebSocket;
        private readonly Request Request;
        private readonly string GroupName;
        public string Id { get; }
        public WebSocketReceiveAction InputHandler { get; set; }
        public WebSocketDisconnectAction DisconnectHandler { get; set; }
        public void HandleInput(string input) => InputHandler?.Invoke(this, input);
        public bool IgnoreOutput { get; set; }
        public string CurrentLocation { get; set; }
        public void SetCurrentLocation(string location) => CurrentLocation = location;
        public IPAddress ClientIpAddress { get; }
        public ITarget Target { get; set; }
        public bool IsShell { get; private set; }

        public void SetShellHandler(WebSocketReceiveAction shellHandler)
        {
            if (InputHandler == null)
            {
                InputHandler = shellHandler;
                IsShell = true;
            }
        }

        public void HandleDisconnect()
        {
            DisconnectHandler?.Invoke(this);
            Status = WebSocketStatus.Closed;
            WebSocketController.HandleDisconnect(Id);
        }

        public WebSocketStatus Status { get; private set; }

        public void Send(string data)
        {
            Send(data.ToBytes());
            Admin.Console.LogWebSocketOutput(this, data);
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
                case WebSocketStatus.Pending:
                    Enqueue(bytes);
                    break;
            }
        }

        private void Enqueue(byte[] data)
        {
            Queue = Queue ?? new ConcurrentQueue<byte[]>();
            Queue.Enqueue(data);
        }

        public void Disconnect() => WebSocket?.Disconnect();
        public override string ToString() => Id;
        public override bool Equals(object obj) => obj is StarcounterWebSocket sws && sws.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();

        public void Open()
        {
            WebSocket = Request.SendUpgrade(GroupName);
            Status = WebSocketStatus.Open;
        }

        public void SendQueuedMessages()
        {
            Queue?.ForEach(message => WebSocket.Send(message));
            Queue = null;
        }

        internal StarcounterWebSocket(string groupName, Request request, string initialLocation)
        {
            GroupName = groupName;
            Request = request;
            Id = request.GetWebSocketId().ToString();
            ClientIpAddress = request.ClientIpAddress;
            Status = WebSocketStatus.Pending;
            CurrentLocation = initialLocation;
        }
    }
}