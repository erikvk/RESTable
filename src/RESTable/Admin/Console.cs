using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Internal.Logging;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Templates;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.Admin
{
    [RESTable(Description = description)]
    internal sealed class Console : FeedTerminal
    {
        private const string description = "The Console is a terminal resource that allows a WebSocket client to receive " +
                                           "pushed updates when the REST API receives requests and WebSocket events.";

        internal const string TypeName = "RESTable.Admin.Console";

        public ConsoleFormat Format { get; set; }
        public bool IncludeClient { get; set; } = true;
        public bool IncludeHeaders { get; set; } = false;
        public bool IncludeContent { get; set; } = false;

        private IWebSocketInternal ActualSocket => ((WebSocketConnection) WebSocket).WebSocket;

        /// <inheritdoc />
        protected override string WelcomeHeader => "RESTable network console";

        /// <inheritdoc />
        protected override string WelcomeBody =>
            "Use the console to receive pushed updates when the \n" +
            "REST API receives requests and WebSocket events.";


        protected override async Task Open(CancellationToken cancellationToken)
        {
            await base.Open(cancellationToken).ConfigureAwait(false);
        }

        #region Console

        private static IJsonProvider? JsonProvider { get; set; }

        internal static async Task Log(IRequest request, ISerializedResult serializedResult)
        {
            JsonProvider ??= request.GetRequiredService<IJsonProvider>();
            var result = serializedResult.Result;
            var milliseconds = result.TimeElapsed.GetRESTableElapsedMs();
            if (result is WebSocketUpgradeSuccessful) return;
            var consoles = request.GetRequiredService<ITerminalCollection<Console>>();
            foreach (var group in consoles.Where(c => c.IsOpen).GroupBy(c => c.Format))
            {
                switch (group.Key)
                {
                    case ConsoleFormat.Line:
                    {
                        var requestStub = await GetLogLineStub(request).ConfigureAwait(false);
                        var responseStub = await GetLogLineStub(result, milliseconds).ConfigureAwait(false);
                        foreach (var console in group)
                        {
                            await console.PrintLines(
                                new StringBuilder(requestStub), request,
                                new StringBuilder(responseStub), result
                            ).ConfigureAwait(false);
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
                                In = new LogItem {Id = request.Context.TraceId, Message = await request.GetLogMessage().ConfigureAwait(false)},
                                Out = new LogItem {Id = result.Context.TraceId, Message = await result.GetLogMessage().ConfigureAwait(false)},
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
                                item.In.Content = await request.GetLogContent().ConfigureAwait(false);
                                item.Out.Content = await serializedResult.GetLogContent().ConfigureAwait(false);
                            }
                            var outputStream = await console.ActualSocket.GetMessageStream(true).ConfigureAwait(false);
#if NETSTANDARD2_0
                            using (outputStream)
#else
                            await using (outputStream)
#endif
                            {
                                await JsonProvider!.SerializeAsync(outputStream, item, prettyPrint: true, ignoreNulls: true).ConfigureAwait(false);
                            }
                        }
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal static async Task Log(IServiceProvider services, ILogable logable)
        {
            var consoles = services.GetRequiredService<ITerminalCollection<Console>>();
            foreach (var group in consoles.Where(c => c.IsOpen).GroupBy(c => c.Format))
            {
                switch (@group.Key)
                {
                    case ConsoleFormat.Line:
                    {
                        var requestStub = await GetLogLineStub(logable).ConfigureAwait(false);
                        foreach (var console in group)
                        {
                            await console.PrintLine(new StringBuilder(requestStub), logable).ConfigureAwait(false);
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
                                Message = await logable.GetLogMessage().ConfigureAwait(false),
                                Time = logable.LogTime
                            };
                            if (console.IncludeClient)
                                item.Client = new ClientInfo(logable.Context.Client);
                            if (console.IncludeHeaders && logable is IHeaderHolder {ExcludeHeaders: false} hh)
                                item.CustomHeaders = hh.Headers;
                            var outputStream = await console.ActualSocket.GetMessageStream(true).ConfigureAwait(false);
#if NETSTANDARD2_0
                            using (outputStream)
#else
                            await using (outputStream)
#endif
                            {
                                await JsonProvider!.SerializeAsync(outputStream, item, prettyPrint: true, ignoreNulls: true).ConfigureAwait(false);
                            }
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
            if (IncludeHeaders && logable is IHeaderHolder {ExcludeHeaders: false} hh)
            {
                builder.Append(headers);
                if (hh.HeadersStringCache is null)
                    hh.HeadersStringCache = string.Join(", ", hh.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                builder.Append(hh.HeadersStringCache);
            }
            if (IncludeContent)
            {
                builder.Append(content);
                builder.Append(await logable.GetLogContent().ConfigureAwait(false) ?? "null");
            }
            await ActualSocket.SendTextRaw(builder.ToString()).ConfigureAwait(false);
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
                if (logable1 is IHeaderHolder {ExcludeHeaders: false} hh1)
                {
                    builder1.Append(headers);
                    if (hh1.HeadersStringCache is null)
                        hh1.HeadersStringCache = string.Join(", ", hh1.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                    builder1.Append(hh1.HeadersStringCache);
                }
                if (logable2 is IHeaderHolder {ExcludeHeaders: false} hh2)
                {
                    builder2.Append(headers);
                    if (hh2.HeadersStringCache is null)
                        hh2.HeadersStringCache = string.Join(", ", hh2.Headers.GetCustom().Select(p => $"{p.Key}: {p.Value}"));
                    builder2.Append(hh2.HeadersStringCache);
                }
            }
            if (IncludeContent)
            {
                builder1.Append(content);
                builder2.Append(content);
                builder1.Append(await logable1.GetLogContent().ConfigureAwait(false) ?? "null");
                builder2.Append(await logable2.GetLogContent().ConfigureAwait(false) ?? "null");
            }
            await ActualSocket.SendTextRaw(builder1.ToString()).ConfigureAwait(false);
            await ActualSocket.SendTextRaw(builder2.ToString()).ConfigureAwait(false);
        }

        private static async Task<string> GetLogLineStub(ILogable logable, double? milliseconds = null)
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
            builder.Append(await logable.GetLogMessage());
            if (milliseconds is not null)
                builder.Append($" ({milliseconds} ms)");
            return builder.ToString();
        }

        #endregion
    }
}