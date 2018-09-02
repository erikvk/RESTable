using System;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Resources.Templates;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Sends feed messages representing generated queries in e.g. SQL
    /// </summary>
    [RESTar]
    public class QueryConsole : FeedTerminal
    {
        private static TerminalSet<QueryConsole> Consoles { get; }
        static QueryConsole() => Consoles = new TerminalSet<QueryConsole>();

        /// <inheritdoc />
        public override void Open() => Consoles.Add(this);

        /// <inheritdoc />
        public override void Dispose() => Consoles.Remove(this);

        internal static void Publish(string kind, string query)
        {
            if (Consoles.Count == 0) return;
            Consoles.ForEach(c => c.WebSocket.SendText($"{DateTime.UtcNow:O}: {kind} : {query}"));
        }
    }
}