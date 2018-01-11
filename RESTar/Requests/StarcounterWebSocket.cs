using System;
using System.IO;
using Starcounter;

namespace RESTar.Requests
{
    internal class StarcounterWebSocket : IWebSocket
    {
        internal WebSocket WebSocket { get; }
        public Action<string> OnReceive { get; set; }
        public string Id => WebSocket.ToUInt64().ToString();
        public StarcounterWebSocket(WebSocket webSocket) => WebSocket = webSocket;
        public void Send(byte[] data) => WebSocket.Send(data);
        public void Send(string data) => WebSocket.Send(data);
        public void Send(Stream data) => WebSocket.Send(data.ToByteArray());
        public void Disconnect() => WebSocket.Disconnect();
    }
}