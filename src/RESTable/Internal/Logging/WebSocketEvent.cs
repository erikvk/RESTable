﻿using System;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Internal.Logging
{
    internal class WebSocketEvent : ILogable
    {
        private readonly string _logMessage;
        private readonly string? _logContent;

        public MessageType MessageType { get; }
        public Headers Headers { get; }
        public string? HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }
        public RESTableContext Context { get; }

        public ValueTask<string> GetLogMessage() => new(_logMessage);
        public ValueTask<string?> GetLogContent() => new(_logContent);

        public WebSocketEvent(MessageType direction, IWebSocket webSocket, string? content = null, long? length = null)
        {
            HeadersStringCache = null!;
            MessageType = direction;
            ExcludeHeaders = false;
            LogTime = DateTime.Now;
            _logMessage = direction switch
            {
                MessageType.WebSocketInput => $"Received {length?.ToString() ?? "a stream of"} bytes",
                MessageType.WebSocketOutput => $"Sent {length?.ToString() ?? "a stream of"} bytes",
                MessageType.WebSocketOpen => "WebSocket opened",
                MessageType.WebSocketClose => "WebSocket closed",
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
            _logContent = content;
            Context = webSocket.Context;
            Headers = webSocket.Headers;
        }
    }
}