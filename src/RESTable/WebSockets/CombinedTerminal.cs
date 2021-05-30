using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Resources;

namespace RESTable.WebSockets
{
    internal class CombinedTerminal<T> : ICombinedTerminal<T>, ITerminalCollection<T> where T : Terminal
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => Terminals.GetEnumerator();
        public int Count => Terminals.Count;
        public IWebSocket CombinedWebSocket { get; }

        private List<T> Terminals { get; }

        /// <summary>
        /// Creates a new <see cref="CombinedTerminal{T}"/> with all terminals from a given terminal collection
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public CombinedTerminal(ITerminalCollection<T> terminals) : this((IEnumerable<T>) terminals) { }

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