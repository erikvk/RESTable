using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.WebSockets
{
    public static class WebSocketExtensions
    {
        /// <summary>
        /// Creates a combined WebSocket interface for the WebSockets of a set of terminals, so that
        /// streams can be sent more efficiently.
        /// </summary>
        public static ICombinedTerminal<T> Combine<T>(this IEnumerable<T> terminals) where T : Terminal
        {
            return new CombinedTerminal<T>(terminals);
        }
    }
}