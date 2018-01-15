using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RESTar.WebSockets
{
    internal static class WebSocketController
    {
        private static readonly IDictionary<string, IWebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, IWebSocket>();
        internal static void Add(IWebSocket webSocket) => AllSockets[webSocket.Id] = webSocket;

        internal static bool TryGet(string wsId, out IWebSocketInternal ws)
        {
            if (AllSockets.TryGetValue(wsId, out var _ws))
            {
                ws = (IWebSocketInternal) _ws;
                return true;
            }
            ws = null;
            return false;
        }

        internal static void HandleDisconnect(string wsId)
        {
            if (!AllSockets.TryGetValue(wsId, out var _)) return;
            AllSockets.Remove(wsId);
        }
    }
}