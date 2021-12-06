using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.WebSockets;
using static RESTable.Method;

namespace RESTable.Admin;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <inheritdoc cref="IAsyncDeleter{T}" />
/// <summary>
///     An entity resource containing all the currently open WebSockets
/// </summary>
[RESTable(GET, DELETE, Description = description)]
public class WebSocket : ISelector<WebSocket>, IAsyncDeleter<WebSocket>
{
    private const string description = "Lists all connected WebSockets";

    private WebSocket(string id, string? terminalType, object? terminal, object client, bool isThis, WebSockets.WebSocket underlyingSocket)
    {
        Id = id;
        TerminalType = terminalType;
        Terminal = terminal;
        Client = client;
        IsThis = isThis;
        UnderlyingSocket = underlyingSocket;
    }

    /// <summary>
    ///     The unique WebSocket ID
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     The type name of the terminal currently connected to the WebSocket
    /// </summary>
    public string? TerminalType { get; }

    /// <summary>
    ///     An object describing the terminal
    /// </summary>
    public object? Terminal { get; }

    /// <summary>
    ///     An object describing the client
    /// </summary>
    public object Client { get; }

    /// <summary>
    ///     Does this WebSocket instance represent the currently connected client websocket?
    /// </summary>
    public bool IsThis { get; }

    private WebSockets.WebSocket UnderlyingSocket { get; }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(IRequest<WebSocket> request, CancellationToken cancellationToken)
    {
        var count = 0;
        await foreach (var entity in request.GetInputEntitiesAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await entity.UnderlyingSocket.DisposeAsync().ConfigureAwait(false);
            count += 1;
        }
        return count;
    }

    /// <inheritdoc />
    public IEnumerable<WebSocket> Select(IRequest<WebSocket> request)
    {
        var webSocketController = request.GetRequiredService<WebSocketManager>();
        return webSocketController
            .ConnectedWebSockets
            .Values
            .Select(socket => new WebSocket
            (
                socket.Context.TraceId,
                isThis: socket.Context.TraceId == request.Context.WebSocket?.Context.TraceId,
                terminalType: socket.TerminalResource?.Name,
                client: socket.GetAppProfile(),
                terminal: socket.Terminal,
                underlyingSocket: socket
            ));
    }
}