using System;
using Newtonsoft.Json;
using RESTable.Requests;

namespace RESTable.Internal.Logging
{
    internal struct InputOutput
    {
        public string Type;
        public ClientInfo? ClientInfo;
        public LogItem In;
        public LogItem Out;
        public double ElapsedMilliseconds;
    }

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

    internal struct LogItem
    {
        public string Type;
        public string Id;
        public string Message;
        public ClientInfo? Client;
        public string Content;
        public Headers CustomHeaders;
        public DateTime? Time;
    }
}