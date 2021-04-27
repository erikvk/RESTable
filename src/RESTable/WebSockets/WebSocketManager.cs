using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.WebSockets
{
    public class WebSocketManager
    {
        internal readonly IDictionary<string, WebSocket> ConnectedWebSockets;
        private IJsonProvider JsonProvider { get; }

        public WebSocketManager(IJsonProvider jsonProvider)
        {
            ConnectedWebSockets = new ConcurrentDictionary<string, WebSocket>();
            JsonProvider = jsonProvider;
        }

        internal void Add(WebSocket webSocket) => ConnectedWebSockets[webSocket.Id] = webSocket;

        internal async Task RevokeAllWithKey(string key)
        {
            var tasks = ConnectedWebSockets.Values
                .Where(webSocket => webSocket.Client.AccessRights.ApiKey == key)
                .Select(webSocket => webSocket.DisposeAsync().AsTask());
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task HandleTextInput(string wsId, string textInput, CancellationToken cancellationToken)
        {
            if (!ConnectedWebSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");

            if (webSocket.IsStreaming)
            {
                await webSocket.HandleStreamingTextInput(textInput).ConfigureAwait(false);
                return;
            }

            if (textInput.ElementAtOrDefault(0) == '#')
            {
                var (command, tail) = textInput.Trim().TupleSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#TERMINAL" when tail is string json:
                        try
                        {
                            JsonProvider.Populate(json, webSocket.Terminal);
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
                            JsonProvider.Populate(json, profile);
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
            else await webSocket.HandleTextInputInternal(textInput, cancellationToken).ConfigureAwait(false);
        }

        public async Task HandleBinaryInput(string wsId, byte[] binaryInput, CancellationToken cancellationToken)
        {
            if (!ConnectedWebSockets.TryGetValue(wsId, out var webSocket))
                throw new UnknownWebSocketIdException($"Unknown WebSocket ID: {wsId}");
            await webSocket.HandleBinaryInputInternal(binaryInput, cancellationToken).ConfigureAwait(false);
        }

        public void RemoveWebSocket(string wsId) => ConnectedWebSockets.Remove(wsId);
    }
}