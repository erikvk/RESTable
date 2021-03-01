using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var list = new List<T>();
            foreach (var terminal in terminals)
            {
                if (terminal.GetWebSocket().Status != WebSocketStatus.Open)
                    throw new ArgumentException($"Cannot combine a set of terminals where at least one has a status other than Open. Found {terminal.GetWebSocket().Status}");
                list.Add(terminal);
            }
            Terminals = list;
            var webSockets = Terminals.Select(terminal => terminal.GetWebSocket());
            CombinedWebSocket = new WebSocketCombination(webSockets);
        }
    }
}