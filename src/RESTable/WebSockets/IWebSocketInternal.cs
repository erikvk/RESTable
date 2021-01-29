using System;

namespace RESTable.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IDisposable
    {
        void SendTextRaw(string text);
        void Disconnect(string message = null);
        void SetStatus(WebSocketStatus status);
    }
}