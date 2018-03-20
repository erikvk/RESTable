using System;
using System.Linq;
using System.Text;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.ResourceTemplates;
using RESTar.Results.Success;
using RESTar.Serialization;
using RESTar.WebSockets;
using static Newtonsoft.Json.Formatting;

namespace RESTar.Admin
{
    [RESTar(Description = description)]
    internal sealed class Console : FeedTerminal
    {
        private const string description = "The Console is a terminal resource that allows a WebSocket client to receive " +
                                           "pushed updates when the REST API receives requests and WebSocket events.";

        internal const string TypeName = "RESTar.Admin.Console";

        private static readonly TerminalSet<Console> Consoles;
        static Console() => Consoles = new TerminalSet<Console>();

        public ConsoleFormat Format { get; set; }
        public bool IncludeConnection { get; set; } = true;
        public bool IncludeHeaders { get; set; } = false;
        public bool IncludeContent { get; set; } = false;

        /// <inheritdoc />
        protected override string WelcomeHeader { get; } = "RESTar network console";

        /// <inheritdoc />
        protected override string WelcomeBody { get; } = "Use the console to receive pushed updates when the \n" +
                                                         "REST API receives requests and WebSocket events.";

        public override void Open()
        {
            base.Open();
            Consoles.Add(this);
        }

        public override void Dispose() => Consoles.Remove(this);

        private IWebSocketInternal WebSocketInternal => (IWebSocketInternal) WebSocket;

        #region Console

        internal static void Log(ILogable initial, ILogable final, double milliseconds)
        {
            if (final is WebSocketResult) return;
            Consoles.AsParallel().Where(c => c.IsOpen).GroupBy(c => c.Format).ForEach(group =>
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                        var requestStub = GetLogLineStub(initial);
                        var responseStub = GetLogLineStub(final, milliseconds);
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
                                Type = "HTTPRequestResponse",
                                In = new LogItem {Id = initial.TraceId, Message = initial.LogMessage},
                                Out = new LogItem {Id = final.TraceId, Message = final.LogMessage},
                                ElapsedMilliseconds = milliseconds
                            };
                            if (c.IncludeConnection)
                                item.ClientInfo = new ClientInfo(initial.Client);
                            if (c.IncludeHeaders)
                            {
                                if (!initial.ExcludeHeaders)
                                    item.In.CustomHeaders = initial.Headers;
                                if (!final.ExcludeHeaders)
                                    item.Out.CustomHeaders = final.Headers;
                            }
                            if (c.IncludeContent)
                            {
                                item.In.Content = initial.LogContent;
                                item.Out.Content = final.LogContent;
                            }
                            var json = Serializers.Json.Serialize(item, Indented, ignoreNulls: true);
                            c.WebSocketInternal.SendTextRaw(json);
                        });
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            });
        }

        internal static void Log(ILogable logable) => Consoles
            .AsParallel()
            .Where(c => c.IsOpen)
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
                                Type = logable.LogEventType.ToString(),
                                Id = logable.TraceId,
                                Message = logable.LogMessage
                            };
                            if (c.IncludeConnection)
                                item.Client = new ClientInfo(logable.Client);
                            else item.Time = logable.LogTime;
                            if (c.IncludeHeaders && !logable.ExcludeHeaders)
                                item.CustomHeaders = logable.Headers;
                            if (c.IncludeContent)
                                item.Content = logable.LogContent;
                            var json = Serializers.Json.Serialize(item, Indented, ignoreNulls: true);
                            c.WebSocketInternal.SendTextRaw(json);
                        });
                        break;
                }
            });


        private const string connection = " | Connection: ";
        private const string headers = " | Custom headers: ";
        private const string content = " | Content: ";

        private void PrintLine(StringBuilder builder, ILogable logable)
        {
            if (IncludeConnection)
            {
                builder.Append(connection);
                builder.Append(logable.Client.ClientIP);
            }
            if (IncludeHeaders && !logable.ExcludeHeaders)
            {
                builder.Append(headers);
                if (logable.HeadersStringCache == null)
                    logable.HeadersStringCache = string.Join(", ", logable.Headers.CustomHeaders.Select(p => $"{p.Key}: {p.Value}"));
                builder.Append(logable.HeadersStringCache);
            }
            if (IncludeContent)
            {
                builder.Append(content);
                builder.Append(logable.LogContent ?? "null");
            }
            WebSocketInternal.SendTextRaw(builder.ToString());
        }

        private void PrintLines(StringBuilder builder1, ILogable logable1, StringBuilder builder2, ILogable logable2)
        {
            if (IncludeConnection)
            {
                builder1.Append(connection);
                builder2.Append(connection);
                builder1.Append(logable1.Client.ClientIP);
                builder2.Append(logable2.Client.ClientIP);
            }
            if (IncludeHeaders)
            {
                if (!logable1.ExcludeHeaders)
                {
                    builder1.Append(headers);
                    if (logable1.HeadersStringCache == null)
                        logable1.HeadersStringCache = string.Join(", ", logable1.Headers.CustomHeaders.Select(p => $"{p.Key}: {p.Value}"));
                    builder1.Append(logable1.HeadersStringCache);
                }
                if (!logable2.ExcludeHeaders)
                {
                    builder2.Append(headers);
                    if (logable2.HeadersStringCache == null)
                        logable2.HeadersStringCache = string.Join(", ", logable2.Headers.CustomHeaders.Select(p => $"{p.Key}: {p.Value}"));
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
            WebSocketInternal.SendTextRaw(builder1.ToString());
            WebSocketInternal.SendTextRaw(builder2.ToString());
        }

        private static string GetLogLineStub(ILogable logable, double? milliseconds = null)
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
            var dateTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ");
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