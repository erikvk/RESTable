using System.Net;
using Starcounter;

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
    public struct Origin
    {
        /// <summary>
        /// The origin type
        /// </summary>
        public OriginType Type { get; }

        /// <summary>
        /// The client IP address that made the request (null for internal requests)
        /// </summary>
        public IPAddress IP { get; }

        /// <summary>
        /// If the client was forwarded by a proxy, this property contains the proxy's IP address. Otherwise null.
        /// </summary>
        public IPAddress Proxy { get; }

        /// <summary>
        /// Creates a new origin with a given type and IP
        /// </summary>
        public Origin(Request request)
        {
            if (request == null)
            {
                Type = OriginType.Internal;
                IP = null;
                Proxy = null;
            }
            else
            {
                string ip = null;
                if (request.HeadersDictionary?.TryGetValue("X-Forwarded-For", out ip) == true && ip != null)
                {
                    IP = IPAddress.Parse(ip.Split(':')[0]);
                    Proxy = request.ClientIpAddress;
                }
                else
                {
                    IP = request.ClientIpAddress;
                    Proxy = null;
                }
                Type = request.IsExternal ? OriginType.External : OriginType.Internal;
            }
        }

        /// <summary>
        /// The internal location
        /// </summary>
        public static Origin Internal;

        /// <summary>
        /// Is the origin internal?
        /// </summary>
        public bool IsInternal => Type == OriginType.Internal;

        /// <summary>
        /// Is the origin external?
        /// </summary>
        public bool IsExternal => Type == OriginType.External;

        static Origin() => Internal = new Origin();
    }
}