using System;
using System.Net;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.WebSockets;

namespace RESTar.Requests
{
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Describes the origin and basic TCP connection parameters of a request
    /// </summary>
    public class Client : ITraceable, IDisposable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        Client ITraceable.Client => this;

        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public IPAddress ClientIP { get; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public IPAddress ProxyIP { get; }

        /// <summary>
        /// The host, as defined in the incoming request
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The user agent, as defined in the incoming request
        /// </summary>
        public string UserAgent { get; }

        /// <summary>
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool HTTPS { get; }

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

        internal string AuthToken { get; set; }

        internal bool IsInShell => Origin == OriginType.WebSocket;

        /// <summary>
        /// Does this TCP connection have a WebSocket?
        /// </summary>
        public bool HasWebSocket => WebSocket != null;

        /// <summary>
        /// Creates a new client with the given origin type
        /// </summary>
        private Client(OriginType origin, string host, IPAddress clientIP, IPAddress proxyIP, string userAgent, bool https)
        {
            TraceId = ConnectionId.Next;
            Origin = origin;
            Host = host;
            ClientIP = clientIP;
            ProxyIP = proxyIP;
            UserAgent = userAgent;
            HTTPS = https;
        }

        internal Client MakeWebSocketClient() => new Client(OriginType.WebSocket, Host, ClientIP, ProxyIP, UserAgent, HTTPS)
            {AuthToken = Authenticator.CloneAuthToken(AuthToken)};

        /// <summary>
        /// Creates a new Client representing an external web client.
        /// </summary>
        /// <param name="clientIP">The IP address of the client</param>
        /// <param name="proxyIP">The IP address of the proxy, or null if no proxy was used to route the request</param>
        /// <param name="userAgent">THe user agent of the client</param>
        /// <param name="host">The content of the host header in the HTTP request</param>
        /// <param name="https">Is the client connected with HTTPS?</param>
        /// <returns></returns>
        public static Client External(IPAddress clientIP, IPAddress proxyIP, string userAgent, string host, bool https) => new Client
        (
            origin: OriginType.External,
            host: host,
            clientIP: clientIP,
            proxyIP: proxyIP,
            userAgent: userAgent,
            https: https
        );

        /// <summary>
        /// The internal location, has root access to all resources
        /// </summary>
        public static Client Internal { get; } = new Client(OriginType.Internal, $"localhost:{Admin.Settings._Port}",
            new IPAddress(new byte[] {127, 0, 0, 1}), null, null, false) {AuthToken = AccessRights.NewRootToken()};

        /// <inheritdoc />
        public void Dispose()
        {
            if (AuthToken == null) return;
            Authenticator.AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}