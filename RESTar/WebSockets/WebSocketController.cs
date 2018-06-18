using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTar.ContentTypeProviders;

namespace RESTar.WebSockets
{
    internal static class WebSocketController
    {
        internal static readonly IDictionary<string, WebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, WebSocket>();
        internal static void Add(WebSocket webSocket) => AllSockets[webSocket.TraceId] = webSocket;

        internal static async Task HandleTextInput(string wsId, string textInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not connected");

            if (webSocket.IsStreaming)
            {
                await webSocket.HandleStreamingTextInput(textInput);
                return;
            }

            if (textInput.ElementAtOrDefault(0) == '#')
            {
                var (command, tail) = textInput.Trim().TSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#TERMINAL" when tail is string json:
                        try
                        {
                            Providers.Json.Populate(json, webSocket.Terminal);
                            webSocket.SendText("Terminal updated");
                            webSocket.SendJson(webSocket.Terminal);
                        }
                        catch (Exception e)
                        {
                            webSocket.SendException(e);
                        }
                        break;
                    case "#TERMINAL":
                        webSocket.SendJson(webSocket.Terminal);
                        break;
                    case "#INFO" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetConnectionProfile();
                            Providers.Json.Populate(json, profile);
                            webSocket.SendText("Profile updated");
                            webSocket.SendJson(webSocket.GetConnectionProfile());
                        }
                        catch (Exception e)
                        {
                            webSocket.SendException(e);
                        }
                        break;
                    case "#INFO":
                        webSocket.SendJson(webSocket.GetConnectionProfile());
                        break;
                    case "#SHELL":
                    case "#HOME":
                        webSocket.DirectToShell();
                        break;
                    case "#DISCONNECT":
                        webSocket.Disconnect();
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
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            webSocket.HandleBinaryInput(binaryInput);
        }

        internal static void HandleDisconnect(string wsId)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            webSocket.Dispose();
            AllSockets.Remove(wsId);
        }
    }
}