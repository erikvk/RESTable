using System;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Internal.Logging
{
    internal class WebSocketEvent : ILogable
    {
        private readonly string _logMessage;
        private readonly string _logContent;
        public MessageType MessageType { get; }
        public string TraceId { get; }

        public ValueTask<string> GetLogMessage() => new(_logMessage);
        public ValueTask<string> GetLogContent() => new(_logContent);

        public Headers Headers { get; }
        public string HeadersStringCache { get; set; }
        private WebSocket WebSocket { get; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }
        public RESTableContext Context { get; }

        public WebSocketEvent(MessageType direction, WebSocket webSocket, string content = null, ulong length = 0)
        {
            MessageType = direction;
            TraceId = webSocket.TraceId;
            WebSocket = webSocket;
            ExcludeHeaders = false;
            LogTime = DateTime.Now;
            switch (direction)
            {
                case MessageType.WebSocketInput:
                    _logMessage = $"Received {length} bytes";
                    break;
                case MessageType.WebSocketOutput:
                    _logMessage = $"Sent {length} bytes";
                    break;
                case MessageType.WebSocketOpen:
                    _logMessage = "WebSocket opened";
                    break;
                case MessageType.WebSocketClose:
                    _logMessage = "WebSocket closed";
                    break;
            }
            _logContent = content;
            Context = webSocket.Context;
            Headers = webSocket.Headers;
        }
    }
}