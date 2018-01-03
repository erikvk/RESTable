using System.Net;

namespace RESTar.Requests
{
    /// <summary>
    /// Describes the origin type of a request
    /// </summary>
    public enum OriginType
    {
        /// <summary>
        /// The request originated from within the RESTar application
        /// </summary>
        Internal,

        /// <summary>
        /// The request originated from outside the RESTar application
        /// </summary>
        External
    }

    /// <summary>
    /// Describes the scheme of a request
    /// </summary>
    public enum Scheme
    {
        /// <summary>
        /// The HTTP scheme
        /// </summary>
        HTTP,

        /// <summary>
        /// The HTTPS scheme
        /// </summary>
        HTTPS
    }

    /// <summary>
    /// Describes the origin and basic parameters of a request
    /// </summary>
    public class Origin
    {
        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Type { get; internal set; }

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
        /// Was the request sent over HTTPS?
        /// </summary>
        public bool HTTPS { get; internal set; }

        internal Origin() { }

        /// <summary>
        /// The internal location
        /// </summary>
        public static readonly Origin Internal = new Origin {Host = $"localhost:{Admin.Settings._Port}"};

        /// <summary>
        /// Is the origin internal?
        /// </summary>
        public bool IsInternal => Type == OriginType.Internal;

        /// <summary>
        /// Is the origin external?
        /// </summary>
        public bool IsExternal => Type == OriginType.External;
    }
}