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

        internal ConnectionProfile(IWebSocketInternal webSocket)
        {
            WebSocketId = webSocket.TraceId;
            Host = webSocket.TcpConnection.Host;
            IsSSLEncrypted = webSocket.TcpConnection.HTTPS;
            ClientIP = webSocket.TcpConnection.ClientIP.ToString();
            ConnectedAt = webSocket.Opened.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentTerminal = webSocket.TerminalResource?.Name ?? "none";
            CustomHeaders = webSocket.Headers;
        }
    }
}