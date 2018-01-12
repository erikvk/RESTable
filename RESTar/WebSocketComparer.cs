using System.Collections.Generic;

namespace RESTar {
    internal class WebSocketComparer : IEqualityComparer<IWebSocket>
    {
        public bool Equals(IWebSocket x, IWebSocket y) => x?.Id == y?.Id;
        public int GetHashCode(IWebSocket obj) => obj.Id.GetHashCode();
    }
}