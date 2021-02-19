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
    internal sealed class Console : FeedTerminal, IDisposable
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

        protected override async Task Open()
        {
            await base.Open();
            Consoles.Add(this);
        }

        public void Dispose() => Consoles.Remove(this);

        #region Console

        internal static async Task Log(IRequest request, ISerializedResult serializedResult)
        {
            var result = serializedResult.Result;
            var milliseconds = result.TimeElapsed.TotalMilliseconds;
            if (result is WebSocketUpgradeSuccessful) return;
            foreach (var group in Consoles.Where(c => c.IsOpen).GroupBy(c => c.Format))
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                    {
                        var requestStub = GetLogLineStub(request);
                        var responseStub = GetLogLineStub(result, milliseconds);
                        foreach (var console in group)
                        {
                            await console.PrintLines(
                                new StringBuilder(requestStub), request,
                                new StringBuilder(responseStub), result
                            );
                        }
                        break;
                    }
                    case ConsoleFormat.JSON:
                    {
                        foreach (var console in group)
                        {
                            var item = new InputOutput
                            {
                                Type = "HTTPRequestResponse",
                                In = new LogItem {Id = request.Context.TraceId, Message = await request.GetLogMessage()},
                                Out = new LogItem {Id = result.Context.TraceId, Message = await result.GetLogMessage()},
                                ElapsedMilliseconds = milliseconds
                            };
                            if (console.IncludeClient)
                                item.ClientInfo = new ClientInfo(request.Context.Client);
                            if (console.IncludeHeaders)
                            {
                                if (!request.ExcludeHeaders)
                                    item.In.CustomHeaders = request.Headers;
                                if (!result.ExcludeHeaders)
                                    item.Out.CustomHeaders = result.Headers;
                            }
                            if (console.IncludeContent)
                            {
                                item.In.Content = await request.GetLogContent();
                                item.Out.Content = await serializedResult.GetLogContent();
                            }
                            var json = Providers.Json.Serialize(item, Indented, ignoreNulls: true);
                            await console.ActualSocket.SendTextRaw(json);
                        }
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal static async Task Log(ILogable logable)
        {
            foreach (var group in Consoles.Where(c => c.IsOpen).GroupBy(c => c.Format))
            {
                switch (@group.Key)
                {
                    case ConsoleFormat.Line:
                    {
                        var requestStub = GetLogLineStub(logable);
                        foreach (var console in group)
                        {
                            await console.PrintLine(new StringBuilder(requestStub), logable);
                        }
                        break;
                    }
                    case ConsoleFormat.JSON:
                    {
                        foreach (var console in group)
                        {
                            var item = new LogItem
                            {
                                Type = logable.MessageType.ToString(),
                                Id = logable.Context.TraceId,
                                Message = await logable.GetLogMessage(),
                                Time = logable.LogTime
                            };
                            if (console.IncludeClient)
                                item.Client = new ClientInfo(logable.Context.Client);
                            if (console.IncludeHeaders && !logable.ExcludeHeaders)
                                item.CustomHeaders = logable.Headers;
                            var json = Providers.Json.Serialize(item, Indented, ignoreNulls: true);
                            await console.ActualSocket.SendTextRaw(json);
                        }
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        private const string connection = " | Connection: ";
        private const string headers = " | Custom headers: ";
        private const string content = " | Content: ";

        private async Task PrintLine(StringBuilder builder, ILogable logable)
        {
            if (IncludeClient)
            {
                builder.Append(connection);
                builder.Append(logable.Context.Client.ClientIp);
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
                builder.Append(await logable.GetLogContent() ?? "null");
            }
            await ActualSocket.SendTextRaw(builder.ToString());
        }

        private async Task PrintLines(StringBuilder builder1, ILogable logable1, StringBuilder builder2, ILogable logable2)
        {
            if (IncludeClient)
            {
                builder1.Append(connection);
                builder2.Append(connection);
                builder1.Append(logable1.Context.Client.ClientIp);
                builder2.Append(logable2.Context.Client.ClientIp);
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
                builder1.Append(await logable1.GetLogContent() ?? "null");
                builder2.Append(await logable2.GetLogContent() ?? "null");
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
            builder.Append($"[{logable.Context.TraceId}] ");
            builder.Append(logable.GetLogMessage());
            if (milliseconds != null)
                builder.Append($" ({milliseconds} ms)");
            return builder.ToString();
        }

        #endregion
    }
}