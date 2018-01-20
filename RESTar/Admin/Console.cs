using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using RESTar.WebSockets;
using static Newtonsoft.Json.NullValueHandling;
using static RESTar.Admin.ConsoleFormat;
using static RESTar.Admin.ConsoleStatus;
using static RESTar.Admin.LogEventType;

namespace RESTar.Admin
{
    internal enum ConsoleStatus
    {
        PAUSED,
        ACTIVE
    }

    internal enum ConsoleFormat
    {
        Line,
        JSON
    }

    internal enum LogEventType
    {
        HttpInput,
        HttpOutput,
        WebSocketInput,
        WebSocketOutput,
        WebSocketOpen,
        WebSocketClose
    }

    internal interface ILogable: ITraceable
    {
        LogEventType LogEventType { get; }
        string LogMessage { get; }
        string LogContent { get; }
        Headers Headers { get; }
        string CustomHeadersString { get; }
    }

    internal struct LogItem
    {
        public string Id;
        public string Message;

        [JsonProperty(NullValueHandling = Ignore)]
        public string Content;

        [JsonProperty(NullValueHandling = Ignore)]
        public string ClientIP;

        [JsonProperty(NullValueHandling = Ignore)]
        public Headers Headers;

        [JsonProperty(NullValueHandling = Ignore)]
        public DateTime? Time;

        public LogItem(ILogable logable)
        {
            Id = logable.TraceId;
            Message = logable.LogMessage;
            Content = logable.LogContent;
            ClientIP = logable.TcpConnection.ClientIP.ToString();
            Headers = logable.Headers;
            Time = null;
        }
    }

    internal class WebSocketEvent : ILogable
    {
        public LogEventType LogEventType { get; }
        public string TraceId { get; }
        public string LogMessage { get; }
        public string LogContent { get; }
        public TCPConnection TcpConnection { get; }
        public Headers Headers { get; }

        private string _chs;
        public string CustomHeadersString => _chs ?? (_chs = string.Join(", ", Headers.CustomHeaders.Select(p => $"{p.Key}: {p.Value}")));

        public WebSocketEvent(LogEventType direction, IWebSocket webSocket, string content = null, int length = 0)
        {
            LogEventType = direction;
            TraceId = webSocket.TraceId;
            switch (direction)
            {
                case WebSocketInput:
                    LogMessage = $"Received {length} bytes";
                    break;
                case WebSocketOutput:
                    LogMessage = $"Sent {length} bytes";
                    break;
                case WebSocketOpen:
                    LogMessage = "WebSocket opened";
                    break;
                case WebSocketClose:
                    LogMessage = "WebSocket closed";
                    break;
            }
            LogContent = content;
            TcpConnection = webSocket.TcpConnection;
            Headers = webSocket.Headers;
        }
    }

    [RESTar(Description = "The console")]
    internal class Console : ITerminal
    {
        public ConsoleStatus Status { get; set; }
        public ConsoleFormat Format { get; set; }
        public bool IncludeClient { get; set; } = true;
        public bool IncludeTime { get; set; } = true;
        public bool IncludeHeaders { get; set; } = false;
        public bool IncludeContent { get; set; } = false;
        public bool ShowWelcomeText { get; set; } = true;

        private IWebSocketInternal WebSocketInternal;

        public void Open()
        {
            Consoles[this] = default;
            Status = PAUSED;
            if (ShowWelcomeText)
                SendConsoleInit();
        }

        public IWebSocket WebSocket
        {
            private get => WebSocketInternal;
            set => WebSocketInternal = (IWebSocketInternal) value;
        }

        public void Dispose() => Consoles.Remove(this);
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        private static string Content(string content) => $" Content: {content}";
        private static string Content(byte[] content) => $" Content: {Encoding.UTF8.GetString(content)}";

        private void SendConsoleInit() => WebSocket
            .SendText("### Welcome to the RESTar WebSocket console! ###\n\n" +
                      ">>> Status: PAUSED\n\n" +
                      "> To begin, type BEGIN\n" +
                      "> To pause, type PAUSE\n" +
                      "> To close, type CLOSE\n");

        public void HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    Status = ACTIVE;
                    WebSocket.SendText("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Status = PAUSED;
                    WebSocket.SendText("Status: PAUSED\n");
                    break;
                case "EXIT":
                case "QUIT":
                case "DISCONNECT":
                case "CLOSE":
                    WebSocket.SendText("Status: CLOSED\n");
                    WebSocket.Disconnect();
                    break;
                case var unrecognized:
                    WebSocket.SendText($"Unknown command '{unrecognized}'");
                    break;
            }
        }

        private static string GetLogLineStub(ILogable logable)
        {
            var builder = new StringBuilder();
            switch (logable.LogEventType)
            {
                case WebSocketInput:
                case HttpInput:
                    builder.Append("=> ");
                    break;
                case HttpOutput:
                case WebSocketOutput:
                    builder.Append("<= ");
                    break;
                case WebSocketOpen:
                    builder.Append("++ ");
                    break;
                case WebSocketClose:
                    builder.Append("-- ");
                    break;
            }
            builder.Append($"[{logable.TraceId}] ");
            builder.Append(logable.LogMessage);
            return builder.ToString();
        }

        internal static void Log(ILogable logable) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .GroupBy(c => c.Format)
            .ForEach(group =>
            {
                switch (group.Key)
                {
                    case Line:
                        var message = GetLogLineStub(logable);
                        group.AsParallel().ForEach(c =>
                        {
                            var builder = new StringBuilder(message);
                            if (c.IncludeClient)
                                builder.Append($" | Client: {logable.TcpConnection.ClientIP}");
                            if (c.IncludeTime)
                                builder.Append($" | Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}");
                            if (c.IncludeHeaders)
                                builder.Append($" | Headers: {logable.CustomHeadersString}");
                            if (c.IncludeContent)
                                builder.Append($" | Content: {logable.LogContent}");
                            c.WebSocketInternal.SendTextRaw(builder.ToString());
                        });
                        break;
                    case JSON:
                        var item = new LogItem(logable);
                        group.AsParallel().ForEach(c =>
                        {
                            var localItem = item;
                            if (!c.IncludeClient)
                                localItem.ClientIP = null;
                            if (c.IncludeTime)
                                localItem.Time = DateTime.Now;
                            if (!c.IncludeHeaders)
                                localItem.Headers = null;
                            if (!c.IncludeContent)
                                localItem.Content = null;
                            var json = JsonConvert.SerializeObject(localItem, Serializer.Settings);
                            c.WebSocketInternal.SendTextRaw(json);
                        });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
                //group.WebSocketInternal.SendTextRaw(message);
            });

        internal static void LogHTTPResult(string requestId, IFinalizedResult result)
        {
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            SendToAll($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                      $"{tail} at {_DateTime}");
        }

        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ff";

        private static string _DateTime => DateTime.Now.ToString(DateTimeFormat);

        private static string TerminalResourcePart(IWebSocketInternal webSocket, string direction) =>
            webSocket.TerminalResource != null ? $"{direction} '{webSocket.TerminalResource.Name}' " : null;

        private const string This = "RESTar.Admin.Console";
        private static readonly IDictionary<Console, byte> Consoles;
        static Console() => Consoles = new ConcurrentDictionary<Console, byte>();

        internal static void LogWebSocketOpen(IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            SendToAll($"++ [WS {webSocket.TraceId}] opened {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {webSocket.Opened.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketClosed(IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            SendToAll($"-- [WS {webSocket.TraceId}] closed {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {webSocket.Closed.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketTextInput(string input, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            var length = Encoding.UTF8.GetByteCount(input);
            SendToAll($"=> [WS {webSocket.TraceId}] received {length} bytes {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}.", input);
        }

        internal static void LogWebSocketBinaryInput(byte[] input, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            SendToAll($"=> [WS {webSocket.TraceId}] received {input.Length} bytes {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}.", input);
        }

        internal static void LogWebSocketTextOutput(string output, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            var length = Encoding.UTF8.GetByteCount(output);
            SendToAll($"<= [WS {webSocket.TraceId}] sent {length} bytes to '{webSocket.TcpConnection.ClientIP}' " +
                      $"{TerminalResourcePart(webSocket, "from")}at {_DateTime}.", output);
        }

        internal static void LogWebSocketBinaryOutput(byte[] output, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.Name == This) return;
            SendToAll($"<= [WS {webSocket.TraceId}] sent {output.Length} bytes to '{webSocket.TcpConnection.ClientIP}' " +
                      $"{TerminalResourcePart(webSocket, "from")}at {_DateTime}.", output);
        }

        private static void SendToAll(string message, string stringContent) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(stringContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message, byte[] bytesContent) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(bytesContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c => c.WebSocketInternal.SendTextRaw(message));
    }
}