using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Internal.Logging;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Templates;
using RESTable.Results;
using RESTable.WebSockets;
using static Newtonsoft.Json.Formatting;

namespace RESTable.Admin
{
    [RESTable(Description = description)]
    internal sealed class Console : FeedTerminal
    {
        private const string description = "The Console is a terminal resource that allows a WebSocket client to receive " +
                                           "pushed updates when the REST API receives requests and WebSocket events.";

        internal const string TypeName = "RESTable.Admin.Console";

        private static readonly TerminalSet<Console> Consoles;
        static Console() => Consoles = new TerminalSet<Console>();

        public ConsoleFormat Format { get; set; }
        public bool IncludeClient { get; set; } = true;
        public bool IncludeHeaders { get; set; } = false;
        public bool IncludeContent { get; set; } = false;

        private IWebSocketInternal ActualSocket => (WebSocket as WebSocketConnection)?.WebSocket;

        /// <inheritdoc />
        protected override string WelcomeHeader { get; } = "RESTable network console";

        /// <inheritdoc />
        protected override string WelcomeBody { get; } = "Use the console to receive pushed updates when the \n" +
                                                         "REST API receives requests and WebSocket events.";

        public override async Task Open()
        {
            await base.Open();
            Consoles.Add(this);
        }

        public override ValueTask DisposeAsync()
        {
            Consoles.Remove(this);
            return default;
        }

        #region Console

        internal static async Task Log(IRequest request, ISerializedResult result)
        {
            var milliseconds = result.TimeElapsed.TotalMilliseconds;
            if (result is WebSocketUpgradeSuccessful) return;
            var tasks = Consoles.Where(c => c.IsOpen).GroupBy(c => c.Format).SelectMany(group =>
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(request);
                        var responseStub = GetLogLineStub(result, milliseconds);
                        return group.Select(c => c.PrintLines(
                            new StringBuilder(requestStub), request,
                            new StringBuilder(responseStub), result)
                        );
                    case ConsoleFormat.JSON:
                        return group.Select(c =>
                        {
                            var item = new InputOutput
                            {
                                Type = "HTTPRequestResponse",
                                In = new LogItem {Id = request.TraceId, Message = request.LogMessage},
                                Out = new LogItem {Id = result.TraceId, Message = result.LogMessage},
                                ElapsedMilliseconds = milliseconds
                            };
                            if (c.IncludeClient)
                                item.ClientInfo = new ClientInfo(request.Context.Client);
                            if (c.IncludeHeaders)
                            {
                                if (!request.ExcludeHeaders)
                                    item.In.CustomHeaders = request.Headers;
                                if (!result.ExcludeHeaders)
                                    item.Out.CustomHeaders = result.Headers;
                            }
                            if (c.IncludeContent)
                            {
                                item.In.Content = request.LogContent;
                                item.Out.Content = result.LogContent;
                            }
                            var json = Providers.Json.Serialize(item, Indented, ignoreNulls: true);
                            return c.ActualSocket.SendTextRaw(json);
                        });
                    default: throw new ArgumentOutOfRangeException();
                }
            });
            await Task.WhenAll(tasks);
        }

        internal static async Task Log(ILogable logable)
        {
            var tasks = Consoles.Where(c => c.IsOpen).GroupBy(c => c.Format).SelectMany(group =>
            {
                switch (@group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(logable);
                        return @group.Select(c => c.PrintLine(new StringBuilder(requestStub), logable));
                    case ConsoleFormat.JSON:
                        return @group.Select(c =>
                        {
                            var item = new LogItem
                            {
                                Type = logable.MessageType.ToString(),
                                Id = logable.TraceId,
                                Message = logable.LogMessage,
                                Time = logable.LogTime
                            };
                            if (c.IncludeClient)
                                item.Client = new ClientInfo(logable.Context.Client);
                            if (c.IncludeHeaders && !logable.ExcludeHeaders)
                                item.CustomHeaders = logable.Headers;
                            if (c.IncludeContent)
                                item.Content = logable.LogContent;
                            var json = Providers.Json.Serialize(item, Indented, ignoreNulls: true);
                            return c.ActualSocket.SendTextRaw(json);
                        });
                    default: throw new ArgumentOutOfRangeException();
                }
            });
            await Task.WhenAll(tasks);
        }

        private const string connection = " | Connection: ";
        private const string headers = " | Custom headers: ";
        private const string content = " | Content: ";

        private async Task PrintLine(StringBuilder builder, ILogable logable)
        {
            if (IncludeClient)
            {
                builder.Append(connection);
                builder.Append(logable.Context.Client.ClientIP);
            }
            if (IncludeHeaders && !logable.ExcludeHeaders)
            {
                builder.Append(headers);
                if (logable.HeadersStringCache == null)
                    logable.HeadersStringCache = string.Join(", ", logable.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                builder.Append(logable.HeadersStringCache);
            }
            if (IncludeContent)
            {
                builder.Append(content);
                builder.Append(logable.LogContent ?? "null");
            }
            await ActualSocket.SendTextRaw(builder.ToString());
        }

        private async Task PrintLines(StringBuilder builder1, ILogable logable1, StringBuilder builder2, ILogable logable2)
        {
            if (IncludeClient)
            {
                builder1.Append(connection);
                builder2.Append(connection);
                builder1.Append(logable1.Context.Client.ClientIP);
                builder2.Append(logable2.Context.Client.ClientIP);
            }
            if (IncludeHeaders)
            {
                if (!logable1.ExcludeHeaders)
                {
                    builder1.Append(headers);
                    if (logable1.HeadersStringCache == null)
                        logable1.HeadersStringCache = string.Join(", ", logable1.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                    builder1.Append(logable1.HeadersStringCache);
                }
                if (!logable2.ExcludeHeaders)
                {
                    builder2.Append(headers);
                    if (logable2.HeadersStringCache == null)
                        logable2.HeadersStringCache = string.Join(", ", logable2.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                    builder2.Append(logable2.HeadersStringCache);
                }
            }
            if (IncludeContent)
            {
                builder1.Append(content);
                builder2.Append(content);
                builder1.Append(logable1.LogContent ?? "null");
                builder2.Append(logable2.LogContent ?? "null");
            }
            await ActualSocket.SendTextRaw(builder1.ToString());
            await ActualSocket.SendTextRaw(builder2.ToString());
        }

        private static string GetLogLineStub(ILogable logable, double? milliseconds = null)
        {
            var builder = new StringBuilder();
            switch (logable.MessageType)
            {
                case MessageType.WebSocketInput:
                case MessageType.HttpInput:
                    builder.Append("=> ");
                    break;
                case MessageType.HttpOutput:
                case MessageType.WebSocketOutput:
                    builder.Append("<= ");
                    break;
                case MessageType.WebSocketOpen:
                    builder.Append("++ ");
                    break;
                case MessageType.WebSocketClose:
                    builder.Append("-- ");
                    break;
            }
            var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            builder.Append(dateTimeString);
            builder.Append($"[{logable.TraceId}] ");
            builder.Append(logable.LogMessage);
            if (milliseconds != null)
                builder.Append($" ({milliseconds} ms)");
            return builder.ToString();
        }

        #endregion
    }
}