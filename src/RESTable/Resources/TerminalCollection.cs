using System.Collections;
using System.Collections.Generic;
using RESTable.WebSockets;

namespace RESTable.Resources;

public class TerminalCollection<T> : ITerminalCollection<T> where T : Terminal
{
    public TerminalCollection(WebSocketManager webSocketManager)
    {
        WebSocketManager = webSocketManager;
    }

    private WebSocketManager WebSocketManager { get; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return GetEnumeration().GetEnumerator();
    }

    private IEnumerable<T> GetEnumeration()
    {
        foreach (var connectedSocket in WebSocketManager.ConnectedWebSockets.Values)
            if (connectedSocket.Terminal is T terminal)
                yield return terminal;
    }
}
