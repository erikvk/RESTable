using System;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.WebSockets;

internal interface IWebSocketInternal : IWebSocket, IAsyncDisposable
{
    Task SendTextRaw(string text, CancellationToken cancellationToken = new());
    void SetStatus(WebSocketStatus status);
}
