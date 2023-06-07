using System;
using RESTable.Requests;

namespace RESTable.WebSockets;

internal class WebSocketContext : RESTableContext
{
    internal WebSocketContext(WebSocket webSocket, Client client, IServiceProvider services) : base(client, services)
    {
        WebSocket = webSocket;
        Client.IsInWebSocket = true;
    }

    protected override bool IsWebSocketUpgrade => false;

    protected override WebSocket CreateServerWebSocket()
    {
        throw new NotImplementedException();
    }
}
