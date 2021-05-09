using System;
using System.Threading.Tasks;
using RESTable.Resources;
using RESTable.Resources.Templates;
using static System.Environment;

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
            await base.Open().ConfigureAwait(false);
            Consoles.Add(this);
        }

        public void Dispose() => Consoles.Remove(this);

        public static async Task Publish(string query, object[] args)
        {
            if (Consoles.Count == 0) return;
            var argsString = args is null ? null : $"{NewLine}Args: {string.Join(", ", args)}";
            var message = $"{DateTime.UtcNow:O}: {query}{argsString}{NewLine}";
            await Consoles.CombinedWebSocket.SendText(message);
        }
    }
}