using System;
using System.Diagnostics;
using System.Net;
using RESTar.WebSockets;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Describes the origin and basic TCP connection parameters of a request
    /// </summary>
    public class TCPConnection : ITraceable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        TCPConnection ITraceable.TcpConnection => this;

        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; internal set; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public IPAddress ClientIP { get; internal set; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public IPAddress ProxyIP { get; internal set; }

        /// <summary>
        /// The host, as defined in the incoming request
        /// </summary>
        public string Host { get; internal set; }

        /// <summary>
        /// The user agent, as defined in the incoming request
        /// </summary>
        public string UserAgent { get; internal set; }

        /// <summary>
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool HTTPS { get; internal set; }

        /// <summary>
        /// The date and time when this connection was opened
        /// </summary>
        public DateTime OpenedAt { get; }

        /// <summary>
        /// The date and time when this connection was closed
        /// </summary>
        public DateTime? ClosedAt { get; internal set; }

        /// <summary>
        /// The internal location
        /// </summary>
        public static readonly TCPConnection Internal = new TCPConnection(OriginType.Internal) {Host = $"localhost:{Admin.Settings._Port}"};

        /// <summary>
        /// Is the origin internal?
        /// </summary>
        public bool IsInternal => Origin == OriginType.Internal;

        /// <summary>
        /// Is the origin external?
        /// </summary>
        public bool IsExternal => Origin == OriginType.External;

        private IWebSocket webSocket;

        /// <summary>
        /// The ID of the websocket connected with this TCP connection
        /// </summary>
        public IWebSocket WebSocket
        {
            get => webSocket;
            internal set
            {
                WebSocketController.Add((IWebSocketInternal) value);
                webSocket = value;
            }
        }

        internal IWebSocketInternal WebSocketInternal => (IWebSocketInternal) WebSocket;

        internal Stopwatch Stopwatch { get; set; }

        internal string AuthToken { get; set; }

        /// <summary>
        /// Does this TCP connection have a WebSocket?
        /// </summary>
        public bool HasWebSocket => WebSocket != null;

        internal TCPConnection(OriginType origin)
        {
            OpenedAt = DateTime.Now;
            TraceId = ConnectionId.Next;
            Origin = origin;
        }
    }
}