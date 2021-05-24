using System.Collections;
using System.Collections.Generic;
using RESTable.WebSockets;

namespace RESTable.Resources
{
    public class TerminalCollection<T> : ITerminalCollection<T> where T : Terminal
    {
        private WebSocketManager WebSocketManager { get; }

        public TerminalCollection(WebSocketManager webSocketManager) => WebSocketManager = webSocketManager;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => GetEnumeration().GetEnumerator();

        private IEnumerable<T> GetEnumeration()
        {
            foreach (var connectedSocket in WebSocketManager.ConnectedWebSockets.Values)
            {
                if (connectedSocket.Terminal is T terminal)
                {
                    yield return terminal;
                }
            }
        }
    }
}