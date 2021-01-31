using System;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Resources;
using RESTable.Resources.Templates;

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
        public override async Task Open()
        {
            await base.Open();
            Consoles.Add(this);
        }

        public override ValueTask DisposeAsync()
        {
            Consoles.Remove(this);
            return default;
        }

        public static async Task Publish(string query, object[] args)
        {
            if (Consoles.Count == 0) return;
            var argsString = args == null ? null : "\r\nArgs: " + string.Join(", ", args);
            var message = $"{DateTime.UtcNow:O}: {query}{argsString}\r\n";
            var tasks = Consoles.Select(c => c.WebSocket.SendText(message));
            await Task.WhenAll(tasks);
        }
    }
}