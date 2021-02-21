using Newtonsoft.Json;
using RESTable.Requests;

namespace RESTable.Internal.Logging
{
    internal struct ClientInfo
    {
        public string ClientIP;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string ProxyIP;

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