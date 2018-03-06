using System;
using Newtonsoft.Json;
using RESTar.Requests;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Logging
{
    internal struct InputOutput
    {
        public string Type;
        public Connection? Connection;
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
        public string Type;
        public string Id;
        public string Message;
        public Connection? Connection;
        public string Content;
        public Headers CustomHeaders;
        public DateTime? Time;
    }
}