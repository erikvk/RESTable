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
        /// Creates a new origin with a given type and IP
        /// </summary>
        public Origin(Request request)
        {
            if (request == null)
            {
                Type = OriginType.Internal;
                IP = null;
            }
            else
            {
                Type = request.IsExternal ? OriginType.External : OriginType.Internal;
                IP = request.ClientIpAddress;
            }
        }

        /// <summary>
        /// The internal location
        /// </summary>
        public static Origin Internal;

        static Origin() => Internal = new Origin();
    }
}