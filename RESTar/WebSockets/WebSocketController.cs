using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTar.Results.Error;
using RESTar.Serialization;

namespace RESTar.WebSockets
{
    internal static class WebSocketController
    {
        private static readonly IDictionary<string, IWebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, IWebSocket>();
        internal static void Add(IWebSocket webSocket) => AllSockets[webSocket.TraceId] = webSocket;

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
                var (command, tail) = textInput.Trim().TSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#SHELL":
                    case "#HOME":
                        Shell.TerminalResource.InstantiateFor(webSocket);
                        break;
                    case "#ME" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetClientProfile();
                            Serializer.Populate(json, profile);
                            webSocket.SendText("Profile updated");
                            webSocket.SendJson(webSocket.GetClientProfile());
                        }
                        catch (Exception e)
                        {
                            webSocket.SendResult(RESTarError.GetError(e));
                        }
                        break;
                    case "#ME":
                        webSocket.SendJson(webSocket.GetClientProfile());
                        break;
                    case "#TERMINAL" when tail is string json:
                        try
                        {
                            var state = webSocket.TerminalResource.GetTerminalState(webSocket.Terminal);
                            Serializer.Populate(json, state);
                            webSocket.TerminalResource.SetTerminalState(state, webSocket.Terminal);
                            webSocket.SendText("Terminal updated");
                            webSocket.SendJson(webSocket.TerminalResource.GetTerminalState(webSocket.Terminal));
                        }
                        catch (Exception e)
                        {
                            webSocket.SendResult(RESTarError.GetError(e));
                        }
                        break;
                    case "#TERMINAL":
                        webSocket.SendJson(webSocket.TerminalResource.GetTerminalState(webSocket.Terminal));
                        break;
                    default:
                        webSocket.SendText($"Unknown global command '{command}'");
                        break;
                }
            }
            else webSocket.HandleTextInput(textInput);
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