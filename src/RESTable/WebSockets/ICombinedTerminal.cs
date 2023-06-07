using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.WebSockets;

public interface ICombinedTerminal<T> : IReadOnlyCollection<T> where T : Terminal
{
    /// <summary>
    ///     A combined websocket that can send messages to a set of actual websockets more efficiently than
    ///     calling the send methods for them individually.
    /// </summary>
    IWebSocket CombinedWebSocket { get; }
}
