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
    internal class WebSocketConnection : IWebSocket, IAsyncDisposable
    {
        private IWebSocketInternal _duringSuspend;
        private IWebSocketInternal _webSocket;

        internal IWebSocketInternal WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnectedException();
            private set => _webSocket = value;
        }

        internal ITerminalResource Resource { get; }
        internal Terminal Terminal { get; private set; }
        public RESTableContext Context { get; private set; }
        private TaskCompletionSource<byte> SuspendTaskSource { get; set; }
        private bool IsSuspended => !SuspendTaskSource.Task.IsCompleted;
        public CancellationToken CancellationToken => WebSocket.CancellationToken;

        internal WebSocketConnection(WebSocket webSocket, Terminal terminal)
        {
            Context = webSocket.Context;
            if (webSocket is null || webSocket.Status == WebSocketStatus.Closed)
                throw new WebSocketNotConnectedException();
            WebSocket = webSocket;
            Resource = terminal.TerminalResource;
            Terminal = terminal;
            Terminal.SetWebSocket(this);
            SuspendTaskSource = new TaskCompletionSource<byte>();
            SuspendTaskSource.SetResult(default);
        }

        internal async Task Suspend()
        {
            if (_duringSuspend != null)
                await _duringSuspend.DisposeAsync().ConfigureAwait(false);
            _duringSuspend = WebSocket;
            SuspendTaskSource = new TaskCompletionSource<byte>();
            WebSocket = new AwaitingWebSocket(_duringSuspend, SuspendTaskSource.Task);
        }

        internal void Unsuspend()
        {
            if (!IsSuspended || WebSocket is not AwaitingWebSocket)
                return;
            WebSocket = _duringSuspend;
            _duringSuspend = null;
            SuspendTaskSource.SetResult(default);
        }

        public async ValueTask DisposeAsync()
        {
            switch (Terminal)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
            WebSocket = null;
            Terminal = null;
            Context = null;
        }

        #region IWebSocket

        /// <inheritdoc />
        public Task SendText(string data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public Task SendText(ArraySegment<byte> buffer) => WebSocket.SendText(buffer);

        /// <inheritdoc />
        public Task SendText(Stream stream) => WebSocket.SendText(stream);

        /// <inheritdoc />
        public Task SendBinary(ArraySegment<byte> buffer) => WebSocket.SendBinary(buffer);

        /// <inheritdoc />
        public Task SendBinary(Stream stream) => WebSocket.SendBinary(stream);

        /// <inheritdoc />
        public Task<Stream> GetMessageStream(bool asText) => WebSocket.GetMessageStream(asText);

        /// <inheritdoc />
        public Task SendJson(object i, bool at = false, bool? p = null, bool ig = false) =>
            WebSocket.SendJson(i, at, p, ig);

        /// <inheritdoc />
        public Task SendResult(IResult r, TimeSpan? t = null, bool w = false) => WebSocket.SendResult(r, t, w);

        /// <inheritdoc />
        public Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? t = null, bool w = false,
            bool d = true) => WebSocket.SendSerializedResult(serializedResult, t, w, d);

        /// <inheritdoc />
        public Task StreamSerializedResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true)
        {
            return WebSocket.StreamSerializedResult(result, messageSize, timeElapsed, writeHeaders, disposeResult);
        }

        /// <inheritdoc />
        public Task SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public ReadonlyCookies Cookies => WebSocket.Cookies;

        /// <inheritdoc />
        public Task DirectToShell(IEnumerable<Condition<Shell>> assignments = null) =>
            WebSocket.DirectToShell(assignments);

        /// <inheritdoc />
        public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>> assignments = null)
            where T : Terminal
        {
            return WebSocket.DirectTo(terminalResource, assignments);
        }

        public string HeadersStringCache
        {
            get => WebSocket.HeadersStringCache;
            set => WebSocket.HeadersStringCache = value;
        }

        public bool ExcludeHeaders => WebSocket.ExcludeHeaders;
        public string ProtocolIdentifier => WebSocket.ProtocolIdentifier;
        public CachedProtocolProvider CachedProtocolProvider => WebSocket.CachedProtocolProvider;

        /// <inheritdoc />
        public WebSocketStatus Status => IsSuspended ? WebSocketStatus.Suspended : WebSocket.Status;

        #endregion
    }
}