using System;
using Newtonsoft.Json;
using RESTar.Requests;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Logging
{
    internal struct RequestResponse
    {
        [JsonProperty(NullValueHandling = Ignore)]
        public Connection? Connection;

        public LogItem Request;
        public LogItem Response;
    }

    internal struct Connection
    {
        public string ClientIP;

        [JsonProperty(NullValueHandling = Ignore)]
        public string ProxyIP;

        public string Protocol;
        public string UserAgent;
        public DateTime? OpenedAt;
        public DateTime? ClosedAt;

        internal Connection(TCPConnection tcpConnection)
        {
            ClientIP = tcpConnection.ClientIP.ToString();
            ProxyIP = tcpConnection.ProxyIP?.ToString();
            Protocol = tcpConnection.HTTPS ? "HTTPS" : "HTTP";
            UserAgent = tcpConnection.UserAgent;
            OpenedAt = tcpConnection.OpenedAt;
            ClosedAt = tcpConnection.ClosedAt;
        }
    }

    internal struct LogItem
    {
        public string Id;
        public string Message;

        [JsonProperty(NullValueHandling = Ignore)]
        public string Content;

        [JsonProperty(NullValueHandling = Ignore)]
        public Headers Headers;

        [JsonProperty(NullValueHandling = Ignore)]
        public string ClientIP;

        [JsonProperty(NullValueHandling = Ignore)]
        public DateTime? Time;

        public LogItem(ILogable logable, bool includeIp)
        {
            Id = logable.TraceId;
            Message = logable.LogMessage;
            Content = logable.LogContent;
            Headers = logable.Headers;
            ClientIP = includeIp ? logable.TcpConnection.ClientIP.ToString() : null;
            Time = null;
        }
    }
}