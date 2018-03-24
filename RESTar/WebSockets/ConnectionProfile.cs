using RESTar.Requests;

namespace RESTar.WebSockets
{
    internal class ConnectionProfile
    {
        public string Host { get; }
        public string WebSocketId { get; }
        public bool IsSSLEncrypted { get; }
        public string ClientIP { get; }
        public string ConnectedAt { get; }
        public string CurrentTerminal { get; }
        public Headers CustomHeaders { get; }

        internal ConnectionProfile(WebSocket webSocket)
        {
            WebSocketId = webSocket.TraceId;
            Host = webSocket.Context.Client.Host;
            IsSSLEncrypted = webSocket.Context.Client.HTTPS;
            ClientIP = webSocket.Context.Client.ClientIP;
            ConnectedAt = webSocket.Opened.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentTerminal = webSocket.TerminalConnection?.Resource?.Name ?? "none";
            CustomHeaders = webSocket.Headers;
        }
    }
}