using System;
using System.Collections.Generic;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar.WebSockets
{
    internal class WebSocketConnection : IWebSocket, IDisposable
    {
        private WebSocket _webSocket;

        internal WebSocket WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnectedException();
            private set => _webSocket = value;
        }

        internal ITerminalResource Resource { get; private set; }
        internal ITerminal Terminal { get; private set; }
        public string TraceId { get; }
        public Context Context { get; private set; }

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
        public void SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public void SendToShell(IEnumerable<Condition<Shell>> assignments = null) => WebSocket.SendToShell(assignments);

        /// <inheritdoc />
        public void SendTo<T>(ITerminalResource<T> terminalResource, IEnumerable<Condition<T>> assignments = null) where T : class, ITerminal =>
            WebSocket.SendTo(terminalResource, assignments);

        /// <inheritdoc />
        public WebSocketStatus Status => WebSocket.Status;

        #endregion
    }
}