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
        internal static readonly IDictionary<string, IWebSocketInternal> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, IWebSocketInternal>();
        internal static void Add(IWebSocketInternal webSocket) => AllSockets[webSocket.TraceId] = webSocket;

        internal static void HandleTextInput(string wsId, string textInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketId($"WebSocket {wsId} no longer connected");
            if (textInput.ElementAtOrDefault(0) == '#')
            {
                var (command, tail) = textInput.Trim().TSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#SHELL":
                    case "#HOME":
                        Shell.TerminalResource.InstantiateFor(webSocket);
                        break;
                    case "#DISCONNECT":
                        webSocket.Disconnect();
                        break;
                    case "#INFO" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetConnectionProfile();
                            Serializers.Json.Populate(json, profile);
                            webSocket.SendText("Profile updated");
                            webSocket.SendJson(webSocket.GetConnectionProfile());
                        }
                        catch (Exception e)
                        {
                            webSocket.SendResult(RESTarError.GetError(e));
                        }
                        break;
                    case "#INFO":
                        webSocket.SendJson(webSocket.GetConnectionProfile());
                        break;
                    case "#TERMINAL" when tail is string json:
                        try
                        {
                            Serializers.Json.Populate(json, webSocket.Terminal);
                            webSocket.SendText("Terminal updated");
                            webSocket.SendJson(webSocket.Terminal);
                        }
                        catch (Exception e)
                        {
                            webSocket.SendResult(RESTarError.GetError(e));
                        }
                        break;
                    case "#TERMINAL":
                        webSocket.SendJson(webSocket.Terminal);
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
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketId($"Unknown WebSocket ID: {wsId}");
            webSocket.HandleBinaryInput(binaryInput);
        }

        internal static void HandleDisconnect(string wsId)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketId($"Unknown WebSocket ID: {wsId}");
            webSocket.Dispose();
            AllSockets.Remove(wsId);
        }
    }
}