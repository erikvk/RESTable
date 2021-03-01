using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.WebSockets
{
    public interface ICombinedTerminal<T> : IReadOnlyCollection<T> where T : Terminal
    {
        IWebSocket CombinedWebSocket { get; }
    }
}