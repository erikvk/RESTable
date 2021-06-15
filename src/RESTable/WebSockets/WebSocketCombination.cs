using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets
{
    internal class WebSocketCombination : IWebSocket
    {
        #region WebSocket

        public RESTableContext Context => ProtocolHolder.Context;
        private IProtocolHolder ProtocolHolder { get; }
        public Headers Headers => null!;

        public string HeadersStringCache
        {
            get => null!;
            set { }
        }

        public bool ExcludeHeaders => false;
        public string ProtocolIdentifier => ProtocolHolder.ProtocolIdentifier;
        public CachedProtocolProvider CachedProtocolProvider => ProtocolHolder.CachedProtocolProvider;
        public ReadonlyCookies Cookies => null!;
        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        public WebSocketStatus Status => WebSocketStatus.Open;

        #endregion

        private CancellationTokenSource CancellationTokenSource { get; }
        private IList<IWebSocket> WebSockets { get; }

        public WebSocketCombination(IEnumerable<IWebSocket> webSockets)
        {
            var empty = true;
            WebSockets = new List<IWebSocket>();
            IProtocolHolder? protocolHolder = null;

            IEnumerable<CancellationToken> iterate()
            {
                foreach (var webSocket in webSockets)
                {
                    empty = false;
                    protocolHolder ??= webSocket;
                    WebSockets.Add(webSocket);
                    yield return webSocket.CancellationToken;
                }
            }

            var cancellationTokens = iterate().ToArray();
            ProtocolHolder = protocolHolder!;
            CancellationTokenSource = empty
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
        }

        private Task DoForAll(Func<IWebSocket, Task> action)
        {
            CancellationToken.ThrowIfCancellationRequested();
            return Task.WhenAll(WebSockets.Select(action));
        }

        public Task SendText(string data) => DoForAll(ws => ws.SendText(data));
        public Task SendText(ArraySegment<byte> buffer) => DoForAll(ws => ws.SendText(buffer));
        public Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false) => DoForAll(ws => ws.SendResult(result, timeElapsed, writeHeaders));
        public Task SendBinary(ArraySegment<byte> buffer) => DoForAll(ws => ws.SendBinary(buffer));
        public Task SendException(Exception exception) => DoForAll(ws => ws.SendException(exception));
        public Task DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => DoForAll(ws => ws.DirectToShell(assignments));

        public async Task<Stream> GetMessageStream(bool asText)
        {
            CancellationToken.ThrowIfCancellationRequested();
            var streams = await Task.WhenAll(WebSockets.Select(ws => ws.GetMessageStream(asText))).ConfigureAwait(false);
            return new CombinedWebSocketsMessageStream(streams, asText, CancellationToken);
        }

        public async Task SendText(Stream stream)
        {
            var messageStream = await GetMessageStream(true).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (messageStream)
#else
            await using (messageStream.ConfigureAwait(false))
#endif
            {
                await stream.CopyToAsync(messageStream, 81920, CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendBinary(Stream stream)
        {
            var messageStream = await GetMessageStream(false).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (messageStream)
#else
            await using (messageStream.ConfigureAwait(false))
#endif
            {
                await stream.CopyToAsync(messageStream, 81920, CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true)
        {
            try
            {
                await SendResult(serializedResult.Result, timeElapsed, writeHeaders).ConfigureAwait(false);
                await SendBinary(serializedResult.Body).ConfigureAwait(false);
            }
            finally
            {
                if (disposeResult)
                    await serializedResult.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false)
        {
            return DoForAll(ws => ws.SendJson(item, asText, prettyPrint, ignoreNulls));
        }

        public Task StreamSerializedResult(ISerializedResult serializedResult, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true)
        {
            throw new NotSupportedException("WebSocket streaming is not supported for combined websockets. Use SendSerializedResult instead");
        }

        public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>> assignments = null) where T : Terminal
        {
            return DoForAll(ws => ws.DirectTo(terminalResource, assignments));
        }
    }
}