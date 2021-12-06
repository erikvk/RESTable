using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Resources;
using RESTable.Resources.Templates;
using RESTable.WebSockets;
using static System.Environment;

namespace RESTable.Admin;

/// <inheritdoc cref="RESTable.Resources.Templates.FeedTerminal" />
/// <inheritdoc cref="System.IDisposable" />
/// <summary>
///     Sends feed messages representing generated queries in e.g. SQL
/// </summary>
[RESTable]
public class QueryConsole : FeedTerminal
{
    public static async Task Publish(string query, CancellationToken cancellationToken, params object[]? args)
    {
        var consoles = ApplicationServicesAccessor.ApplicationServiceProvider.GetRequiredService<ICombinedTerminal<QueryConsole>>();
        if (consoles.Count == 0) return;
        var argsString = args is null ? null : $"{NewLine}Args: {string.Join(", ", args)}";
        var message = $"{DateTime.UtcNow:O}: {query}{argsString}{NewLine}";
        await consoles.CombinedWebSocket.SendText(message, cancellationToken).ConfigureAwait(false);
    }
}