﻿using System.Collections.Generic;

namespace RESTable.WebSockets
{
    internal class WebSocketComparer : IEqualityComparer<IWebSocket>
    {
        public bool Equals(IWebSocket x, IWebSocket y) => x?.TraceId == y?.TraceId;
        public int GetHashCode(IWebSocket obj) => obj.TraceId.GetHashCode();
    }
}