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
    /// Describes the origin of a request
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
        public IPAddress IP { get; internal set; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public IPAddress Proxy { get; internal set; }

        internal Origin() { }

        /// <summary>
        /// The internal location
        /// </summary>
        public static readonly Origin Internal = new Origin();

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