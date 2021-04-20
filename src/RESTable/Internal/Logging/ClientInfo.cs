using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Internal.Logging
{
    internal struct ClientInfo
    {
        public string ClientIP;

        [RESTableMember(hideIfNull: true)] 
        public string ProxyIP { get; set; }

        public string Protocol;
        public string UserAgent;

        internal ClientInfo(Client client)
        {
            ClientIP = client.ClientIp;
            ProxyIP = client.ProxyIp;
            Protocol = client.Https ? "HTTPS" : "HTTP";
            UserAgent = client.UserAgent;
        }
    }
}