using System;
using System.IO;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar
{
    internal class WebSocketConnection : IWebSocket, IDisposable
    {
        private WebSocket _webSocket;

        internal WebSocket WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnected();
            private set => _webSocket = value;
        }

        internal ITerminalResource TerminalResource { get; }
        internal ITerminal Terminal { get; }
        internal bool IsOpen { get; private set; }

        public string TraceId { get; }
        public Context Context { get; }

        internal WebSocketConnection(WebSocket webSocket, ITerminalResource terminalResource, ITerminal terminal)
        {
            TraceId = webSocket.Id;
            Context = webSocket.Context;
            if (webSocket == null || webSocket.Status == WebSocketStatus.Closed)
                throw new WebSocketNotConnected();
            WebSocket = webSocket;
            TerminalResource = terminalResource;
            Terminal = terminal;
            Terminal.WebSocket = this;
            IsOpen = true;
        }

        public void Dispose()
        {
            WebSocket = null;
            Terminal.Dispose();
            IsOpen = false;
        }

        #region IWebSocket

        /// <inheritdoc />
        public void SendText(string data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public void SendText(byte[] data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public void SendText(Stream data) => WebSocket.SendText(data);

        /// <inheritdoc />
        public void SendBinary(byte[] data) => WebSocket.SendBinary(data);

        /// <inheritdoc />
        public void SendBinary(Stream data) => WebSocket.SendBinary(data);

        /// <inheritdoc />
        public void SendJson(object i, bool? p = null, bool ig = false) => WebSocket.SendJson(i, p, ig);

        /// <inheritdoc />
        public void SendResult(IFinalizedResult r, bool i = true, TimeSpan? t = null) => WebSocket.SendResult(r, i);

        /// <inheritdoc />
        public void SendException(Exception exception) => WebSocket.SendException(exception);

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public void SendToShell() => WebSocket.SendToShell();

        /// <inheritdoc />
        public void SendTo(ITerminalResource terminalResource) => WebSocket.SendTo(terminalResource);

        /// <inheritdoc />
        public WebSocketStatus Status => WebSocket.Status;

        #endregion
    }
}