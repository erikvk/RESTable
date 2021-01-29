using System;
using System.Threading.Tasks;
using RESTable.Resources;
using RESTable.Resources.Templates;
using RESTable.Linq;

namespace RESTable.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Sends feed messages representing generated queries in e.g. SQL
    /// </summary>
    [RESTable]
    public class QueryConsole : FeedTerminal
    {
        private static TerminalSet<QueryConsole> Consoles { get; }
        static QueryConsole() => Consoles = new TerminalSet<QueryConsole>();

        /// <inheritdoc />
        public override void Open()
        {
            base.Open();
            Consoles.Add(this);
        }

        /// <inheritdoc />
        public override void Dispose() => Consoles.Remove(this);

        public static void Publish(string query, object[] args)
        {
            if (Consoles.Count == 0) return;
            var argsString = args == null ? null : "\r\nArgs: " + string.Join(", ", args);
            var message = $"{DateTime.UtcNow:O}: {query}{argsString}\r\n";
            Task.Run(() => Consoles.ForEach(c => c.WebSocket.SendText(message)));
        }
    }
}