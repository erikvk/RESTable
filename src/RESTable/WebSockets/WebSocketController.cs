using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.WebSockets
{
    public static class WebSocketController
    {
        internal static readonly IDictionary<string, WebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, WebSocket>();
        internal static void Add(WebSocket webSocket) => AllSockets[webSocket.TraceId] = webSocket;

        internal static async Task RevokeAllWithKey(string key)
        {
            var tasks = AllSockets.Values
                .Where(webSocket => webSocket.Client.AccessRights.ApiKey == key)
                .Select(webSocket => webSocket.DisposeAsync().AsTask());
            await Task.WhenAll(tasks);
        }

        public static async Task HandleTextInput(string wsId, string textInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");

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
                            await webSocket.SendText("Terminal updated");
                            await webSocket.SendJson(webSocket.Terminal);
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e);
                        }
                        break;
                    case "#TERMINAL":
                        await webSocket.SendJson(webSocket.Terminal);
                        break;
                    case "#INFO" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetAppProfile();
                            Providers.Json.Populate(json, profile);
                            await webSocket.SendText("Profile updated");
                            await webSocket.SendJson(webSocket.GetAppProfile());
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e);
                        }
                        break;
                    case "#INFO":
                        await webSocket.SendJson(webSocket.GetAppProfile());
                        break;
                    case "#SHELL":
                    case "#HOME":
                        await webSocket.DirectToShell();
                        break;
                    case "#DISCONNECT":
                        await webSocket.DisposeAsync();
                        break;
                    default:
                        await webSocket.SendText($"Unknown global command '{command}'");
                        break;
                }
            }
            else await webSocket.HandleTextInput(textInput);
        }

        public static async Task HandleBinaryInput(string wsId, byte[] binaryInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            await webSocket.HandleBinaryInput(binaryInput);
        }

        public static void RemoveWebSocket(string wsId) => AllSockets.Remove(wsId);
    }
}