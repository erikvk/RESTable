using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.WebSockets
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="IWebSocketInternal" />
    /// /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// A WebSocket wrapper that puts all void returning operations in a queue for later execution
    /// </summary>
    internal class WebSocketQueue : IWebSocket, IWebSocketInternal, IDisposable
    {
        internal ConcurrentQueue<Action> ActionQueue { get; }
        internal IWebSocketInternal ToQueueFor { get; }

        public string TraceId { get; }
        public Context Context { get; }
        public WebSocketStatus Status => ToQueueFor.Status;

        /// <inheritdoc />
        public void SetStatus(WebSocketStatus status) => ActionQueue.Enqueue(() => ToQueueFor.SetStatus(status));

        public Headers Headers => ToQueueFor.Headers;

        public WebSocketQueue(IWebSocketInternal webSocket)
        {
            ActionQueue = new ConcurrentQueue<Action>();
            ToQueueFor = webSocket;
            TraceId = webSocket.TraceId;
            Context = webSocket.Context;
        }

        public void SendTextRaw(string text) => ActionQueue.Enqueue(() => ToQueueFor.SendTextRaw(text));
        public void SendText(string d) => ActionQueue.Enqueue(() => ToQueueFor.SendText(d));
        public void SendText(byte[] d, int o, int l) => ActionQueue.Enqueue(() => ToQueueFor.SendText(d, o, l));
        public void SendBinary(byte[] d, int o, int l) => ActionQueue.Enqueue(() => ToQueueFor.SendBinary(d, o, l));
        public void SendJson(object i, bool a = false, bool? p = null, bool ig = false) => ActionQueue.Enqueue(() => ToQueueFor.SendJson(i, a, p, ig));
        public void SendException(Exception exception) => ActionQueue.Enqueue(() => ToQueueFor.SendException(exception));

        public void SendResult(ISerializedResult r, TimeSpan? t = null, bool w = false, bool d = true) =>
            ActionQueue.Enqueue(() => ToQueueFor.SendResult(r, t, w, d));

        public void StreamResult(ISerializedResult r, int m, TimeSpan? t = null, bool w = false, bool d = true) =>
            ActionQueue.Enqueue(() => ToQueueFor.StreamResult(r, m, t, w, d));

        public void DirectToShell(IEnumerable<Condition<Shell>> a = null) => ActionQueue.Enqueue(() => ToQueueFor.DirectToShell(a));

        public void DirectTo<T>(ITerminalResource<T> t, IEnumerable<Condition<T>> a = null) where T : class, ITerminal =>
            ActionQueue.Enqueue(() => ToQueueFor.DirectTo(t, a));

        public void Disconnect() => ActionQueue.Enqueue(() => ToQueueFor.Disconnect());

        public void Dispose() => ActionQueue.Enqueue(() =>
        {
            var webSocket = (WebSocket) ToQueueFor;
            webSocket.Dispose();
        });
    }

    internal class WebSocketConnection : IWebSocket, IDisposable
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
        public Context Context { get; private set; }

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

        internal void Suspend()
        {
            if (IsSuspended) return;
            duringSuspend?.Dispose();
            WebSocket.SetStatus(WebSocketStatus.Suspended);
            duringSuspend = WebSocket;
            WebSocket = new WebSocketQueue(duringSuspend);
            IsSuspended = true;
        }

        internal void Unsuspend()
        {
            if (!IsSuspended || !(WebSocket is WebSocketQueue queue)) return;
            IsSuspended = false;
            duringSuspend.SetStatus(WebSocketStatus.Open);
            WebSocket = duringSuspend;
            duringSuspend = null;
            queue.ActionQueue.ForEach(a => a());
        }

        public void Dispose()
        {
            Terminal.Dispose();
            WebSocket = null;
            Resource = null;
            Terminal = null;
            Context = null;
        }

        #region IWebSocket

        /// <inheritdoc />
        public void SendText(string data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public void SendText(byte[] data, int offset, int length) => WebSocket.SendText(data, offset, length);

        /// <inheritdoc />
        public void SendBinary(byte[] data, int offset, int length) => WebSocket.SendBinary(data, offset, length);

        /// <inheritdoc />
        public void SendJson(object i, bool at = false, bool? p = null, bool ig = false) => WebSocket.SendJson(i, at, p, ig);

        /// <inheritdoc />
        public void SendResult(ISerializedResult r, TimeSpan? t = null, bool w = false, bool d = true) => WebSocket.SendResult(r, t, w, d);

        /// <inheritdoc />
        public void StreamResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false,
            bool disposeResult = true) => WebSocket.StreamResult(result, messageSize, timeElapsed, writeHeaders, disposeResult);

        /// <inheritdoc />
        public void SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public void DirectToShell(IEnumerable<Condition<Shell>> assignments = null) => WebSocket.DirectToShell(assignments);

        /// <inheritdoc />
        public void DirectTo<T>(ITerminalResource<T> terminalResource, IEnumerable<Condition<T>> assignments = null) where T : class, ITerminal =>
            WebSocket.DirectTo(terminalResource, assignments);

        /// <inheritdoc />
        public WebSocketStatus Status => WebSocket.Status;

        #endregion
    }
}