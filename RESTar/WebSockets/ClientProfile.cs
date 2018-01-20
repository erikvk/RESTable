using RESTar.Requests;

namespace RESTar.WebSockets
{
    internal class ClientProfile
    {
        public string WebSocketId { get; }
        public string IPAddress { get; }
        public string ConnectedAt { get; }
        public string CurrentTerminal { get; }
        public Headers CustomHeaders { get; }

        internal ClientProfile(IWebSocketInternal webSocket)
        {
            WebSocketId = webSocket.TraceId;
            IPAddress = webSocket.TcpConnection.ClientIP.ToString();
            ConnectedAt = webSocket.Opened.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentTerminal = webSocket.TerminalResource?.Name ?? "none";
            CustomHeaders = webSocket.Headers;
        }
    }
}