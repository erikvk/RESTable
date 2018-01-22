using System;
using Newtonsoft.Json;
using RESTar.Requests;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Logging
{
    internal struct InputOutput
    {
        public string Type;

        [JsonProperty(NullValueHandling = Ignore)]
        public Connection? Connection;

        public LogItem In;
        public LogItem Out;
    }

    internal struct Connection
    {
        public string ClientIP;

        [JsonProperty(NullValueHandling = Ignore)]
        public string ProxyIP;

        public string Protocol;
        public string UserAgent;
        public DateTime? OpenedAt;

        internal Connection(TCPConnection tcpConnection, bool includeTimes)
        {
            ClientIP = tcpConnection.ClientIP.ToString();
            ProxyIP = tcpConnection.ProxyIP?.ToString();
            Protocol = tcpConnection.HTTPS ? "HTTPS" : "HTTP";
            UserAgent = tcpConnection.UserAgent;
            if (includeTimes)
                OpenedAt = tcpConnection.OpenedAt;
            else OpenedAt = null;
        }
    }

    internal struct LogItem
    {
        [JsonProperty(NullValueHandling = Ignore)]
        public string Type;

        public string Id;
        public string Message;

        [JsonProperty(NullValueHandling = Ignore)]
        public Connection? Connection;

        [JsonProperty(NullValueHandling = Ignore)]
        public string Content;

        [JsonProperty(NullValueHandling = Ignore)]
        public Headers CustomHeaders;

        [JsonProperty(NullValueHandling = Ignore)]
        public DateTime? Time;
    }
}