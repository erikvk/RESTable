using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Internal.Logging
{
    internal readonly struct ClientInfo
    {
        public string? ClientIP { get; }

        [RESTableMember(hideIfNull: true)]
        public string? ProxyIP { get; }

        public string? Protocol { get; }
        public string? UserAgent { get; }

        internal ClientInfo(Client client)
        {
            ClientIP = client.ClientIp;
            ProxyIP = client.ProxyIp;
            Protocol = client.Https ? "HTTPS" : "HTTP";
            UserAgent = client.UserAgent;
        }
    }
}