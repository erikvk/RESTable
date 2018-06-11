using System;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IDisposable
    {
        void SendTextRaw(string text);
        void Disconnect();
        void SetStatus(WebSocketStatus status);
    }
}