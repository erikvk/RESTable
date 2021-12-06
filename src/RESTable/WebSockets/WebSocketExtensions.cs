using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.WebSockets;

public static class WebSocketExtensions
{
    /// <summary>
    ///     Creates a combined WebSocket interface for the WebSockets of a set of terminals, so that
    ///     streams can be sent more efficiently.
    /// </summary>
    public static ICombinedTerminal<T> Combine<T>(this IEnumerable<T> terminals) where T : Terminal
    {
        return CombinedTerminal<T>.Create(terminals);
    }

    /// <summary>
    ///     Creates a combined WebSocket interface for the WebSockets of a set of terminals, so that
    ///     streams can be sent more efficiently.
    /// </summary>
    public static async ValueTask<ICombinedTerminal<T>> CombineAsync<T>(this IAsyncEnumerable<T> terminals, CancellationToken cancellationToken) where T : Terminal
    {
        var terminalList = await terminals.ToListAsync(cancellationToken).ConfigureAwait(false);
        return CombinedTerminal<T>.Create(terminalList);
    }
}