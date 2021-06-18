using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using RESTable.Auth;
using RESTable.Meta;

namespace RESTable.Requests
{
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Describes the origin and basic client parameters of a request
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Origin { get; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public string? ClientIp { get; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public string? ProxyIp { get; }

        /// <summary>
        /// The host, as defined in the incoming request
        /// </summary>
        public string? Host { get; }

        /// <summary>
        /// The user agent, as defined in the incoming request
        /// </summary>
        public string? UserAgent { get; }

        /// <summary>
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool Https { get; }

        /// <summary>
        /// The cookies associated with this client
        /// </summary>
        public Cookies Cookies { get; }

        /// <summary>
        /// The API access rights of this client
        /// </summary>
        public AccessRights AccessRights { get; }

        internal IDictionary<IResource, byte> ResourceAuthMappings { get; }
        internal IDictionary<IResource, IDictionary<string, object?>> ResourceClientDataMappings { get; }
        internal bool IsInWebSocket { get; set; }
        internal string? ShellConfig { get; set; }

        /// <summary>
        /// Creates a new client with the given origin type
        /// </summary>
        public Client
        (
            OriginType origin,
            string? host,
            IPAddress clientIp,
            IPAddress? proxyIp,
            string? userAgent,
            bool https,
            Cookies cookies,
            AccessRights accessRights
        )
        {
            Origin = origin;
            Host = host;
            ClientIp = clientIp.ToString();
            ProxyIp = proxyIp?.ToString();
            UserAgent = userAgent;
            Https = https;
            ResourceAuthMappings = new ConcurrentDictionary<IResource, byte>();
            ResourceClientDataMappings = new ConcurrentDictionary<IResource, IDictionary<string, object?>>();
            Cookies = cookies;
            AccessRights = accessRights;
        }

        /// <summary>
        /// Creates a new Client representing an external web client.
        /// </summary>
        /// <param name="clientIp">The IP address of the client</param>
        /// <param name="proxyIp">The IP address of the proxy, or null if no proxy was used to route the request</param>
        /// <param name="userAgent">THe user agent of the client</param>
        /// <param name="host">The content of the host header in the HTTP request</param>
        /// <param name="https">Is the client connected with HTTPS?</param>
        /// <param name="cookies">The cookies registered for this client</param>
        /// <returns></returns>
        public static Client External
        (
            IPAddress clientIp,
            IPAddress? proxyIp,
            string? userAgent,
            string? host,
            bool https,
            Cookies? cookies,
            AccessRights accessRights
        ) => new
        (
            origin: OriginType.External,
            host: host,
            clientIp: clientIp,
            proxyIp: proxyIp,
            userAgent: userAgent,
            https: https,
            cookies: cookies ?? new Cookies(),
            accessRights: accessRights
        );

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Origin: {Origin}, IP: {ClientIp}";
        }
    }
}