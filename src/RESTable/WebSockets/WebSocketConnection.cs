﻿using System;
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
        private IWebSocketInternal? _duringSuspend;
        private IWebSocketInternal _webSocket;

        internal IWebSocketInternal WebSocket
        {
            get => _webSocket ?? throw new WebSocketNotConnectedException();
            private set => _webSocket = value;
        }

        internal ITerminalResource? Resource { get; }
        internal Terminal Terminal { get; private set; }
        public RESTableContext Context { get; private set; }
        private TaskCompletionSource<byte> SuspendTaskSource { get; set; }

        private bool IsSuspended => !SuspendTaskSource.Task.IsCompleted;

        internal WebSocketConnection(WebSocket webSocket, Terminal terminal)
        {
            Context = webSocket.Context;
            if (webSocket is null || webSocket.Status == WebSocketStatus.Closed)
                throw new WebSocketNotConnectedException();
            _webSocket = webSocket;
            Resource = terminal.TerminalResource;
            Terminal = terminal;
            Terminal.SetWebSocket(this);
            SuspendTaskSource = new TaskCompletionSource<byte>();
            SuspendTaskSource.SetResult(default);
        }

        internal async Task Suspend()
        {
            if (_duringSuspend is not null)
                await _duringSuspend.DisposeAsync().ConfigureAwait(false);
            _duringSuspend = WebSocket;
            SuspendTaskSource = new TaskCompletionSource<byte>();
            WebSocket = new AwaitingWebSocket(_duringSuspend, SuspendTaskSource.Task);
        }

        internal void Unsuspend()
        {
            if (!IsSuspended || WebSocket is not AwaitingWebSocket)
                return;
            WebSocket = _duringSuspend!;
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
            WebSocket = null!;
            Terminal = null!;
            Context = null!;
        }

        #region IWebSocket

        /// <inheritdoc />
        public Task SendText(string data, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendText(data, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendText(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendText(buffer, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendText(Stream stream, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendText(stream, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendBinary(ArraySegment<byte> buffer, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendBinary(buffer, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendBinary(Stream stream, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendBinary(stream, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new())
        {
            return WebSocket.GetMessageStream(asText, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendJson(object dataObject, bool at = false, bool? p = null, bool ig = false, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendJson(dataObject, at, p, ig, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendResult(IResult r, TimeSpan? t = null, bool w = false, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendResult(r, t, w, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? t = null, bool w = false,
            bool d = true, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendSerializedResult(serializedResult, t, w, d, cancellationToken);
        }

        /// <inheritdoc />
        public Task StreamSerializedResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true, CancellationToken cancellationToken = new())
        {
            return WebSocket.StreamSerializedResult(result, messageSize, timeElapsed, writeHeaders, disposeResult, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendException(Exception exception, CancellationToken cancellationToken = new())
        {
            return WebSocket.SendException(exception, cancellationToken);
        }

        /// <inheritdoc />
        public Headers Headers => WebSocket.Headers;

        /// <inheritdoc />
        public ReadonlyCookies Cookies => WebSocket.Cookies;

        /// <inheritdoc />
        public Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new())
        {
            return WebSocket.DirectToShell(assignments, cancellationToken);
        }

        /// <inheritdoc />
        public Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new())
            where T : Terminal
        {
            return WebSocket.DirectTo(terminalResource, assignments, cancellationToken);
        }

        public string? HeadersStringCache
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