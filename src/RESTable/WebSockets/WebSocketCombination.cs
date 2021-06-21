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
        #region Not supported for WebSocketCombination

        public RESTableContext Context => throw new NotSupportedException();
        private IProtocolHolder ProtocolHolder => throw new NotSupportedException();
        public Headers Headers => throw new NotSupportedException();

        public string? HeadersStringCache
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public bool ExcludeHeaders => throw new NotSupportedException();
        public string ProtocolIdentifier => throw new NotSupportedException();
        public CachedProtocolProvider CachedProtocolProvider => throw new NotSupportedException();
        public ReadonlyCookies Cookies => throw new NotSupportedException();

        #endregion

        public WebSocketStatus Status => WebSocketStatus.Open;

        private IWebSocket[] WebSockets { get; }

        public WebSocketCombination(IWebSocket[] webSockets)
        {
            WebSockets = webSockets;
        }

        private Task DoForAll(Func<IWebSocket, Task> action)
        {
            return Task.WhenAll(WebSockets.Select(action));
        }

        public Task SendText(string data, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendText(data, cancellationToken));
        }

        public Task SendText(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendText(buffer, cancellationToken));
        }

        public Task SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendResult(result, timeElapsed, writeHeaders, cancellationToken));
        }

        public Task SendBinary(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendBinary(buffer, cancellationToken));
        }

        public Task SendException(Exception exception, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendException(exception, cancellationToken));
        }

        public Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.DirectToShell(assignments, cancellationToken));
        }

        public async Task<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new())
        {
            var streams = await Task.WhenAll(WebSockets.Select(ws => ws.GetMessageStream(asText, cancellationToken))).ConfigureAwait(false);
            return new CombinedWebSocketsMessageStream(streams, asText, cancellationToken);
        }

        public async Task SendText(Stream stream, CancellationToken cancellationToken = new())
        {
            var messageStream = await GetMessageStream(true, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (messageStream)
#else
            await using (messageStream.ConfigureAwait(false))
#endif
            {
                await stream.CopyToAsync(messageStream, 81920, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendBinary(Stream stream, CancellationToken cancellationToken = new())
        {
            var messageStream = await GetMessageStream(false, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (messageStream)
#else
            await using (messageStream.ConfigureAwait(false))
#endif
            {
                await stream.CopyToAsync(messageStream, 81920, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true,
            CancellationToken cancellationToken = new())
        {
            try
            {
                await SendResult(serializedResult.Result, timeElapsed, writeHeaders, cancellationToken).ConfigureAwait(false);
                await SendBinary(serializedResult.Body, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (disposeResult)
                    await serializedResult.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new())
        {
            return DoForAll(ws => ws.SendJson(item, asText, prettyPrint, ignoreNulls, cancellationToken));
        }

        public Task StreamSerializedResult(ISerializedResult serializedResult, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true, CancellationToken cancellationToken = new())
        {
            throw new NotSupportedException("WebSocket streaming is not supported for combined websockets. Use SendSerializedResult instead");
        }

        public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new())
            where T : Terminal
        {
            return DoForAll(ws => ws.DirectTo(terminalResource, assignments, cancellationToken));
        }
    }
}