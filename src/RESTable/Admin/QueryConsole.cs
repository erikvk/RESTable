using System;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Resources;
using RESTable.Resources.Templates;

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Templates.FeedTerminal" />
    /// <inheritdoc cref="System.IDisposable" />
    /// <summary>
    /// Sends feed messages representing generated queries in e.g. SQL
    /// </summary>
    [RESTable]
    public class QueryConsole : FeedTerminal, IDisposable
    {
        private static TerminalSet<QueryConsole> Consoles { get; }
        static QueryConsole() => Consoles = new TerminalSet<QueryConsole>();

        /// <inheritdoc />
        protected override async Task Open()
        {
            await base.Open();
            Consoles.Add(this);
        }

        public void Dispose() => Consoles.Remove(this);

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