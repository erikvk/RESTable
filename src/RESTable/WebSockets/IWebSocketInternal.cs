using System;
using System.Threading.Tasks;

namespace RESTable.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IAsyncDisposable
    {
        Task SendTextRaw(string text);
        Task SetStatus(WebSocketStatus status);
    }
}