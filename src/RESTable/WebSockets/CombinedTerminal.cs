using System.Collections;
using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.WebSockets
{
    internal class CombinedTerminal<T> : ICombinedTerminal<T> where T : Terminal
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => Terminals.GetEnumerator();
        public int Count => Terminals.Count;
        public IWebSocket CombinedWebSocket { get; }

        private List<T> Terminals { get; }

        public CombinedTerminal(IEnumerable<T> terminals)
        {
            Terminals = new List<T>();
            var webSockets = new List<IWebSocket>();
            foreach (var terminal in terminals)
            {
                Terminals.Add(terminal);
                webSockets.Add(terminal.GetWebSocket());
            }
            CombinedWebSocket = new WebSocketCombination(webSockets);
        }
    }
}