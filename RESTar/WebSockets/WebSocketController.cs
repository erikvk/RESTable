using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RESTar.WebSockets
{
    internal static class WebSocketController
    {
        private static readonly IDictionary<string, IWebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, IWebSocket>();
        internal static void Add(IWebSocket webSocket) => AllSockets[webSocket.Id] = webSocket;

        private static bool TryGet(string wsId, out IWebSocketInternal ws)
        {
            if (AllSockets.TryGetValue(wsId, out var _ws))
            {
                ws = (IWebSocketInternal) _ws;
                return true;
            }
            ws = null;
            return false;
        }

        internal static void HandleTextInput(string wsId, string textInput)
        {
            if (!TryGet(wsId, out var webSocket))
                throw new UnknownWebSocketId($"Unknown WebSocket ID: {wsId}");
            if (textInput.ElementAtOrDefault(0) == '#')
            {
                switch (textInput.Trim().ToUpperInvariant())
                {
                    case "#HOME":
                        webSocket.EnterShell();
                        return;
                }
            }
            webSocket.HandleTextInput(textInput);
        }

        internal static void HandleBinaryInput(string wsId, byte[] binaryInput)
        {
            if (!TryGet(wsId, out var webSocket))
                throw new UnknownWebSocketId($"Unknown WebSocket ID: {wsId}");
            webSocket.HandleBinaryInput(binaryInput);
        }

        internal static void HandleDisconnect(string wsId)
        {
            if (!TryGet(wsId, out var webSocket))
                throw new UnknownWebSocketId($"Unknown WebSocket ID: {wsId}");
            webSocket.Dispose();
            AllSockets.Remove(wsId);
        }
    }
}