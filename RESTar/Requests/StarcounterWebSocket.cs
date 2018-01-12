using System.Collections.Concurrent;
using System.IO;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Serialization;
using Starcounter;

namespace RESTar.Requests
{
    internal interface IWebSocketInternal
    {
        void Open();
    }

    internal class StarcounterWebSocket : IWebSocket, IWebSocketInternal
    {
        private readonly ConcurrentQueue<string> Queue;
        private WebSocket WebSocket;
        private readonly Request Request;
        private readonly string GroupName;
        public string Id { get; }
        public WebSocketReceiveAction InputHandler { get; set; }
        public WebSocketDisconnectAction DisconnectHandler { get; set; }
        
        public void Send(string data)
        {
            if (WebSocket == null)
                Queue.Enqueue(data);
            else WebSocket.Send(data);
        }

        public void Send(Stream data)
        {
            if (WebSocket == null)
                Queue.Enqueue(data.GetString());
            else WebSocket?.Send(data.ToByteArray());
        }

        public void Disconnect() => WebSocket?.Disconnect();
        public override string ToString() => Id;
        public override bool Equals(object obj) => obj is StarcounterWebSocket sws && sws.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();

        public void Open()
        {
            WebSocket = Request.SendUpgrade(GroupName);
            Queue.ForEach(message => WebSocket.Send(message));
        }

        internal StarcounterWebSocket(string groupName, Request request, WebSocketReceiveAction inputHandler)
        {
            GroupName = groupName;
            Request = request;
            InputHandler = inputHandler;
            Id = request.GetWebSocketId().ToString();
            Queue = new ConcurrentQueue<string>();
        }
    }
}