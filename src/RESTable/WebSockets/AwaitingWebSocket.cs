using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="IWebSocketInternal" />
    /// /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// A WebSocket wrapper that awaits a wait task before forwarding calls 
    /// </summary>
    internal class AwaitingWebSocket : IWebSocket, IWebSocketInternal
    {
        private IWebSocketInternal WebSocket { get; }
        private Task WaitTask { get; }

        public RESTableContext Context { get; }
        public WebSocketStatus Status => WebSocket.Status;
        public Headers Headers => WebSocket.Headers;
        public ReadonlyCookies Cookies => WebSocket.Cookies;

        /// <inheritdoc />
        public async void SetStatus(WebSocketStatus status)
        {
            await WaitTask.ConfigureAwait(false);
            WebSocket.SetStatus(status);
        }

        public AwaitingWebSocket(IWebSocketInternal webSocket, Task waitTask)
        {
            WaitTask = waitTask;
            WebSocket = webSocket;
            Context = webSocket.Context;
        }

        async Task IWebSocketInternal.SendTextRaw(string text, CancellationToken cancellationToken)
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendTextRaw(text, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendText(string d, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendText(d, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendText(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendText(buffer, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendText(Stream stream, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendText(stream, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendBinary(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendBinary(buffer, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendBinary(Stream stream, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendBinary(stream, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            return await WebSocket.GetMessageStream(asText, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendJson(object i, bool a = false, bool? p = null, bool ig = false, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendJson(i, a, p, ig, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendException(Exception exception, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendException(exception, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? t = null, bool w = false, bool d = true, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendSerializedResult(serializedResult, t, w, d, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendResult(IResult r, TimeSpan? t = null, bool w = false, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.SendResult(r, t, w, cancellationToken).ConfigureAwait(false);
        }

        public async Task StreamSerializedResult(ISerializedResult r, int m, TimeSpan? t = null, bool w = false, bool d = true, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.StreamSerializedResult(r, m, t, w, d, cancellationToken).ConfigureAwait(false);
        }

        public async Task DirectToShell(ICollection<Condition<Shell>>? a = null, CancellationToken cancellationToken = new())
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.DirectToShell(a, cancellationToken).ConfigureAwait(false);
        }

        public async Task DirectTo<T>(ITerminalResource<T> t, ICollection<Condition<T>>? a = null, CancellationToken cancellationToken = new()) where T : Terminal
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.DirectTo(t, a, cancellationToken).ConfigureAwait(false);
        }

        public string? HeadersStringCache
        {
            get => WebSocket.HeadersStringCache;
            set => WebSocket.HeadersStringCache = value;
        }

        public async ValueTask DisposeAsync()
        {
            await WaitTask.ConfigureAwait(false);
            await WebSocket.DisposeAsync().ConfigureAwait(false);
        }

        public bool ExcludeHeaders => WebSocket.ExcludeHeaders;
        public string ProtocolIdentifier => WebSocket.ProtocolIdentifier;
        public CachedProtocolProvider CachedProtocolProvider => WebSocket.CachedProtocolProvider;
    }
}