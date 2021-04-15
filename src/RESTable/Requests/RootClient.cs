using System.Net;
using RESTable.Internal.Auth;

namespace RESTable.Requests
{
    /// <summary>
    /// The root client, capable of accessing all resources
    /// </summary>
    public class RootClient : Client
    {
        public RootClient(RootAccess rootAccess) : base
        (
            origin: OriginType.Internal,
            host: "localhost",
            clientIp: new IPAddress(new byte[] {127, 0, 0, 1}),
            proxyIp: null,
            userAgent: null,
            https: false,
            cookies: new Cookies()
        )
        {
            AccessRights = rootAccess;
        }
    }
}