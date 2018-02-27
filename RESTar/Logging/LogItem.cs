using System;
using Newtonsoft.Json;
using RESTar.Requests;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Logging
{
    internal struct InputOutput
    {
        public string Type;

        [JsonProperty(NullValueHandling = Ignore)] public Connection? Connection;

        public LogItem In;
        public LogItem Out;
        public double ElapsedMilliseconds;
    }

    internal struct Connection
    {
        public string ClientIP;

        [JsonProperty(NullValueHandling = Ignore)] public string ProxyIP;

        public string Protocol;
        public string UserAgent;
        public DateTime? OpenedAt;

        internal Connection(TCPConnection tcpConnection)
        {
            ClientIP = tcpConnection.ClientIP.ToString();
            ProxyIP = tcpConnection.ProxyIP?.ToString();
            Protocol = tcpConnection.HTTPS ? "HTTPS" : "HTTP";
            UserAgent = tcpConnection.UserAgent;
            OpenedAt = tcpConnection.OpenedAt;
        }
    }

    internal struct LogItem
    {
        [JsonProperty(NullValueHandling = Ignore)] public string Type;

        public string Id;
        public string Message;

        [JsonProperty(NullValueHandling = Ignore)] public Connection? Connection;
        [JsonProperty(NullValueHandling = Ignore)] public string Content;
        [JsonProperty(NullValueHandling = Ignore)] public Headers CustomHeaders;
        [JsonProperty(NullValueHandling = Ignore)] public DateTime? Time;
    }
}