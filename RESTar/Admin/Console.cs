using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Serialization;
using RESTar.WebSockets;

namespace RESTar.Admin
{
    [RESTar(Description = "The console")]
    internal class Console : ITerminal
    {
        internal const string TypeName = "RESTar.Admin.Console";

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

        private const string connection = " | Connection: ";
        private const string time = " | Time: ";
        private const string headers = " | Headers: ";
        private const string content = " | Content: ";

        private void PrintLine(StringBuilder builder, ILogable logable)
        {
            if (IncludeConnection)
            {
                builder.Append(connection);
                builder.Append(logable.TcpConnection.ClientIP);
            }
            if (IncludeTime)
            {
                var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff");
                builder.Append(time);
                builder.Append(dateTimeString);
            }
            if (IncludeHeaders)
            {
                builder.Append(headers);
                builder.Append(logable.CustomHeadersString);
            }
            if (IncludeContent)
            {
                builder.Append(content);
                builder.Append(logable.LogContent);
            }
            WebSocketInternal.SendTextRaw(builder.ToString());
        }

        private void PrintLines(StringBuilder builder1, ILogable logable1, StringBuilder builder2, ILogable logable2)
        {
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

        internal static void Log(ILogable initial, ILogable final) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .GroupBy(c => c.Format)
            .ForEach(group =>
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(initial);
                        var responseStub = GetLogLineStub(final);
                        group.AsParallel().ForEach(c => c.PrintLines(
                            new StringBuilder(requestStub), initial,
                            new StringBuilder(responseStub), final)
                        );
                        break;
                    case ConsoleFormat.JSON:
                        group.AsParallel().ForEach(c =>
                        {
                            var item = new InputOutput
                            {
                                In = new LogItem {Id = initial.TraceId, Message = initial.LogMessage},
                                Out = new LogItem {Id = final.TraceId, Message = final.LogMessage}
                            };
                            if (c.IncludeConnection)
                                item.Connection = new Connection(initial.TcpConnection, c.IncludeTime);
                            else if (c.IncludeTime)
                            {
                                item.In.Time = initial.TcpConnection.OpenedAt;
                                item.Out.Time = initial.TcpConnection.ClosedAt;
                            }
                            if (c.IncludeHeaders)
                            {
                                item.In.Headers = initial.Headers;
                                item.Out.Headers = final.Headers;
                            }
                            if (c.IncludeContent)
                            {
                                item.In.Content = initial.LogContent;
                                item.Out.Content = final.LogContent;
                            }
                            var json = JsonConvert.SerializeObject(item, Serializer.Settings);
                            c.WebSocketInternal.SendTextRaw(json);
                        });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            });

        internal static void Log(ILogable logable) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ConsoleStatus.ACTIVE)
            .GroupBy(c => c.Format)
            .ForEach(group =>
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(logable);
                        group.AsParallel().ForEach(c => c.PrintLine(new StringBuilder(requestStub), logable));
                        break;
                    case ConsoleFormat.JSON:
                        group.AsParallel().ForEach(c =>
                        {
                            var item = new LogItem
                            {
                                Id = logable.TraceId,
                                Message = logable.LogMessage
                            };
                            if (c.IncludeConnection)
                                item.Connection = new Connection(logable.TcpConnection, c.IncludeTime);
                            else if (c.IncludeTime)
                                item.Time = logable.TcpConnection.OpenedAt;
                            if (c.IncludeHeaders)
                                item.Headers = logable.Headers;
                            if (c.IncludeContent)
                                item.Content = logable.LogContent;
                            var json = JsonConvert.SerializeObject(item, Serializer.Settings);
                            c.WebSocketInternal.SendTextRaw(json);
                        });
                        break;
                }
            });

        private static readonly IDictionary<Console, byte> Consoles;
        static Console() => Consoles = new ConcurrentDictionary<Console, byte>();
    }
}