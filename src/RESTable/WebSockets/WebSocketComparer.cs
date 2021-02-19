using System.Collections.Generic;

namespace RESTable.WebSockets
{
    internal class WebSocketComparer : IEqualityComparer<IWebSocket>
    {
        public bool Equals(IWebSocket x, IWebSocket y) => x?.Context.Equals(y?.Context) == true;
        public int GetHashCode(IWebSocket obj) => obj.Context.GetHashCode();
    }
}