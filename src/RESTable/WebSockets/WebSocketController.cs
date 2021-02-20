using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.WebSockets
{
    internal static class WebSocketController
    {
        internal static readonly IDictionary<string, WebSocket> AllSockets;
        static WebSocketController() => AllSockets = new ConcurrentDictionary<string, WebSocket>();
        internal static void Add(WebSocket webSocket) => AllSockets[webSocket.Id] = webSocket;

        internal static async Task RevokeAllWithKey(string key)
        {
            var tasks = AllSockets.Values
                .Where(webSocket => webSocket.Client.AccessRights.ApiKey == key)
                .Select(webSocket => webSocket.DisposeAsync().AsTask());
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public static async Task HandleTextInput(string wsId, string textInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");

            if (webSocket.IsStreaming)
            {
                await webSocket.HandleStreamingTextInput(textInput).ConfigureAwait(false);
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
                            await webSocket.SendText("Terminal updated").ConfigureAwait(false);
                            await webSocket.SendJson(webSocket.Terminal).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e).ConfigureAwait(false);
                        }
                        break;
                    case "#TERMINAL":
                        await webSocket.SendJson(webSocket.Terminal).ConfigureAwait(false);
                        break;
                    case "#INFO" when tail is string json:
                        try
                        {
                            var profile = webSocket.GetAppProfile();
                            Providers.Json.Populate(json, profile);
                            await webSocket.SendText("Profile updated").ConfigureAwait(false);
                            await webSocket.SendJson(webSocket.GetAppProfile()).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e).ConfigureAwait(false);
                        }
                        break;
                    case "#INFO":
                        await webSocket.SendJson(webSocket.GetAppProfile()).ConfigureAwait(false);
                        break;
                    case "#SHELL":
                    case "#HOME":
                        await webSocket.DirectToShell().ConfigureAwait(false);
                        break;
                    case "#DISCONNECT":
                        await webSocket.DisposeAsync().ConfigureAwait(false);
                        break;
                    default:
                        await webSocket.SendText($"Unknown global command '{command}'").ConfigureAwait(false);
                        break;
                }
            }
            else await webSocket.HandleTextInputInternal(textInput).ConfigureAwait(false);
        }

        public static async Task HandleBinaryInput(string wsId, byte[] binaryInput)
        {
            if (!AllSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            await webSocket.HandleBinaryInputInternal(binaryInput).ConfigureAwait(false);
        }

        public static void RemoveWebSocket(string wsId) => AllSockets.Remove(wsId);
    }
}