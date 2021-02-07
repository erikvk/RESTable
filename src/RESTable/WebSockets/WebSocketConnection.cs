using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets
{
    internal class WebSocketConnection : IWebSocket, IAsyncDisposable
    {
        private IWebSocketInternal duringSuspend;
        private IWebSocketInternal _webSocket;

        internal IWebSocketInternal WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnectedException();
            private set => _webSocket = value;
        }

        internal ITerminalResource Resource { get; private set; }
        internal ITerminal Terminal { get; private set; }
        public string TraceId { get; }
        public RESTableContext Context { get; private set; }

        private bool IsSuspended { get; set; }

        internal WebSocketConnection(WebSocket webSocket, ITerminal terminal, ITerminalResource resource)
        {
            TraceId = webSocket.Id;
            Context = webSocket.Context;
            if (webSocket == null || webSocket.Status == WebSocketStatus.Closed)
                throw new WebSocketNotConnectedException();
            WebSocket = webSocket;
            Resource = resource;
            Terminal = terminal;
            Terminal.WebSocket = this;
        }

        internal async Task Suspend()
        {
            if (IsSuspended) return;
            if (duringSuspend != null)
                await duringSuspend.DisposeAsync();
            duringSuspend = WebSocket;
            WebSocket = new WebSocketQueue(duringSuspend);
            IsSuspended = true;
        }

        internal async Task Unsuspend()
        {
            if (!IsSuspended || !(WebSocket is WebSocketQueue queue))
                return;
            IsSuspended = false;
            WebSocket = duringSuspend;
            duringSuspend = null;
            var tasks = queue.ActionQueue.Select(a => a());
            await Task.WhenAll(tasks);
        }

        public async ValueTask DisposeAsync()
        {
            await Terminal.DisposeAsync();
            WebSocket = null;
            Resource = null;
            Terminal = null;
            Context = null;
        }

        #region IWebSocket

        /// <inheritdoc />
        public Task SendText(string data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public Task SendText(byte[] data, int offset, int length) => WebSocket.SendText(data, offset, length);

        /// <inheritdoc />
        public Task SendBinary(byte[] data, int offset, int length) => WebSocket.SendBinary(data, offset, length);

        /// <inheritdoc />
        public Task SendJson(object i, bool at = false, bool? p = null, bool ig = false) => WebSocket.SendJson(i, at, p, ig);

        /// <inheritdoc />
        public Task SendResult(IResult r, TimeSpan? t = null, bool w = false, bool d = true) => WebSocket.SendResult(r, t, w, d);

        /// <inheritdoc />
        public Task StreamResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true)
        {
            return WebSocket.StreamResult(result, messageSize, timeElapsed, writeHeaders, disposeResult);
        }

        /// <inheritdoc />
        public Task SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public ReadonlyCookies Cookies => WebSocket.Cookies;

        /// <inheritdoc />
        public Task DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => WebSocket.DirectToShell(assignments);

        /// <inheritdoc />
        public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>> assignments = null) where T : class, ITerminal
        {
            return WebSocket.DirectTo(terminalResource, assignments);
        }

        /// <inheritdoc />
        public WebSocketStatus Status => IsSuspended ? WebSocketStatus.Suspended : WebSocket.Status;

        #endregion
    }
}