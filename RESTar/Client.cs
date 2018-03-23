using System;
using System.Net;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Describes the origin and basic TCP connection parameters of a request
    /// </summary>
    public class Client : IDisposable, ITraceable
    {
        public string TraceId { get; internal set; }
        public Context Context { get; internal set; }

        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public string ClientIP { get; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public string ProxyIP { get; }

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

        internal string AuthToken { get; set; }

        internal bool IsInWebSocket { get; set; }

        /// <summary>
        /// Creates a new client with the given origin type
        /// </summary>
        private Client(OriginType origin, string host, IPAddress clientIP, IPAddress proxyIP, string userAgent, bool https)
        {
            Origin = origin;
            Host = host;
            ClientIP = clientIP.ToString();
            ProxyIP = proxyIP.ToString();
            UserAgent = userAgent;
            HTTPS = https;
        }

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
            if (AuthToken == null || IsInWebSocket) return;
            Authenticator.AuthTokens.TryRemove(AuthToken, out var _);
        }
    }
}