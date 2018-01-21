using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Serialization;
using RESTar.WebSockets;

namespace RESTar.Admin
{
    [RESTar(Description = "The console")]
    internal class Console : ITerminal
    {
        public ConsoleStatus Status { get; set; }
        public ConsoleFormat Format { get; set; }
        public bool IncludeConnection { get; set; } = true;
        public bool IncludeTime { get; set; } = true;
        public bool IncludeHeaders { get; set; } = false;
        public bool IncludeContent { get; set; } = false;
        public bool ShowWelcomeText { get; set; } = true;

        private IWebSocketInternal WebSocketInternal;

        public void Open()
        {
            Consoles[this] = default;
            Status = ConsoleStatus.PAUSED;
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
                    Status = ConsoleStatus.ACTIVE;
                    WebSocket.SendText("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Status = ConsoleStatus.PAUSED;
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

        private void PrintLines(StringBuilder builder1, ILogable logable1, StringBuilder builder2, ILogable logable2)
        {
            const string connection = " | Connection: ";
            const string time = " | Time: ";
            const string headers = " | Headers: ";
            const string content = " | Content: ";

            if (IncludeConnection)
            {
                builder1.Append(connection);
                builder2.Append(connection);
                builder1.Append(logable1.TcpConnection.ClientIP);
                builder2.Append(logable2.TcpConnection.ClientIP);
            }
            if (IncludeTime)
            {
                var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff");
                builder1.Append(time);
                builder2.Append(time);
                builder1.Append(dateTimeString);
                builder2.Append(dateTimeString);
            }
            if (IncludeHeaders)
            {
                builder1.Append(headers);
                builder2.Append(headers);
                builder1.Append(logable1.CustomHeadersString);
                builder2.Append(logable2.CustomHeadersString);
            }
            if (IncludeContent)
            {
                builder1.Append(content);
                builder2.Append(content);
                builder1.Append(logable1.LogContent);
                builder2.Append(logable2.LogContent);
            }
            WebSocketInternal.SendTextRaw(builder1.ToString());
            WebSocketInternal.SendTextRaw(builder2.ToString());
        }

        private static string GetLogLineStub(ILogable logable)
        {
            var builder = new StringBuilder();
            switch (logable.LogEventType)
            {
                case LogEventType.WebSocketInput:
                case LogEventType.HttpInput:
                    builder.Append("=> ");
                    break;
                case LogEventType.HttpOutput:
                case LogEventType.WebSocketOutput:
                    builder.Append("<= ");
                    break;
                case LogEventType.WebSocketOpen:
                    builder.Append("++ ");
                    break;
                case LogEventType.WebSocketClose:
                    builder.Append("-- ");
                    break;
            }
            builder.Append($"[{logable.TraceId}] ");
            builder.Append(logable.LogMessage);
            return builder.ToString();
        }

        internal static void Log(ILogable httpRequest, ILogable httpResponse) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .GroupBy(c => c.Format)
            .ForEach(group =>
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(httpRequest);
                        var responseStub = GetLogLineStub(httpResponse);
                        group.AsParallel().ForEach(c => c.PrintLines(
                            new StringBuilder(requestStub), httpRequest,
                            new StringBuilder(responseStub), httpResponse)
                        );
                        break;
                    case ConsoleFormat.JSON:
                        var item = new RequestResponse
                        {
                            Connection = new Connection(httpRequest.TcpConnection),
                            Request = new LogItem(httpRequest, false),
                            Response = new LogItem(httpResponse, false)
                        };
                        group.AsParallel().ForEach(c =>
                        {
                            var localItem = item;
                            if (!c.IncludeConnection)
                            {
                                localItem.Connection = null;
                                if (c.IncludeTime)
                                {
                                    localItem.Request.Time = httpRequest.TcpConnection.OpenedAt;
                                    localItem.Response.Time = httpRequest.TcpConnection.ClosedAt;
                                }
                            }
                            if (!c.IncludeHeaders)
                            {
                                localItem.Request.Headers = null;
                                localItem.Response.Headers = null;
                            }
                            if (!c.IncludeContent)
                            {
                                localItem.Request.Content = null;
                                localItem.Response.Content = null;
                            }
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
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(stringContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message, byte[] bytesContent) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(bytesContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .ForEach(c => c.WebSocketInternal.SendTextRaw(message));
    }
}