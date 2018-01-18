using System.Collections.Generic;
using System.Linq;
using static System.StringComparer;

namespace RESTar.WebSockets
{
    internal class ClientProfile
    {
        public string WebSocketId { get; }
        public string IPAddress { get; }
        public string ConnectedAt { get; }
        public string CurrentTerminal { get; }
        public IDictionary<string, string> CustomHeaders { get; private set; }

        internal ClientProfile(IWebSocketInternal webSocket)
        {
            WebSocketId = webSocket.Id;
            IPAddress = webSocket.TcpConnection.ClientIP.ToString();
            ConnectedAt = webSocket.Opened.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentTerminal = webSocket.TerminalResource?.FullName ?? "none";
            CustomHeaders = webSocket.Headers
                .Where(IsAvailable)
                .ToDictionary(p => p.Key, p => p.Value, OrdinalIgnoreCase);
        }

        internal void ClearUnavailableHeaders() => CustomHeaders = CustomHeaders
            .Where(IsAvailable)
            .ToDictionary(p => p.Key, p => p.Value, OrdinalIgnoreCase);

        private static bool IsAvailable(KeyValuePair<string, string> header)
        {
            switch (header.Key)
            {
                case "Sec-WebSocket-Version":
                case "Sec-WebSocket-Key":
                case "Connection":
                case "Upgrade":
                case "Authorization":
                case "Sec-WebSocket-Extensions":
                case "RESTar-AuthToken":
                case "Host": return false;
                default: return true;
            }
        }
    }
}