using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Resources;

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
                .Where(webSocket => webSocket.Client.AccessRights.Token == key)
                .Select(webSocket => webSocket.DisposeAsync().AsTask());
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task HandleTextInput(string wsId, string textInput, CancellationToken cancellationToken)
        {
            if (!ConnectedWebSockets.TryGetValue(wsId, out var webSocket) || webSocket is not WebSocket)
            {
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");
            }

            if (webSocket.Terminal is not Terminal terminal)
            {
                await webSocket.DisposeAsync().ConfigureAwait(false);
                throw new Exception($"Cannot handle text input for WebSocket '{wsId}' with no attached terminal");
            }

            if (await HandleGlobalCommand(terminal, webSocket, textInput, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await webSocket.HandleTextInputInternal(textInput, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks the text input for the global command pattern, and executes the command and returns true
        /// if the input encoded a global command.
        /// </summary>
        private async Task<bool> HandleGlobalCommand(Terminal terminal, WebSocket webSocket, string textInput, CancellationToken cancellationToken)
        {
            Task sendJson(object obj) => webSocket.Send
            (
                data: JsonProvider.SerializeToUtf8Bytes(obj, obj.GetType(), true, true),
                asText: true,
                cancellationToken
            );

            if (textInput.ElementAtOrDefault(0) == '#' && char.IsLetter(textInput.ElementAtOrDefault(1)))
            {
                var (command, tail) = textInput.Trim().TupleSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "#TERMINAL" when tail is string json:
                    {
                        try
                        {
                            await JsonProvider.PopulateAsync(terminal, json).ConfigureAwait(false);
                            await webSocket.SendText("Terminal updated", cancellationToken).ConfigureAwait(false);
                            await sendJson(terminal).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e, cancellationToken).ConfigureAwait(false);
                        }
                        return true;
                    }
                    case "#TERMINAL":
                    {
                        await sendJson(terminal).ConfigureAwait(false);
                        return true;
                    }
                    case "#OPTIONS":
                    {
                        await sendJson(new {terminal.SupportsTextInput, terminal.SupportsBinaryInput}).ConfigureAwait(false);
                        return true;
                    }
                    case "#INFO" when tail is string json:
                    {
                        try
                        {
                            var profile = webSocket.GetAppProfile();
                            await JsonProvider.PopulateAsync(profile, json).ConfigureAwait(false);
                            foreach (var (key, value) in profile.CustomHeaders)
                                webSocket.Headers[key] = value;
                            await webSocket.SendText("Profile updated", cancellationToken).ConfigureAwait(false);
                            await sendJson(webSocket.GetAppProfile()).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            await webSocket.SendException(e, cancellationToken).ConfigureAwait(false);
                        }
                        return true;
                    }
                    case "#INFO":
                    {
                        await sendJson(webSocket.GetAppProfile()).ConfigureAwait(false);
                        return true;
                    }
                    case "#SHELL":
                    case "#HOME":
                    {
                        await webSocket.DirectToShell(cancellationToken: cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    case "#DISCONNECT":
                    {
                        await webSocket.DisposeAsync().ConfigureAwait(false);
                        return true;
                    }
                }
            }
            return false;
        }

        public Task HandleBinaryInput(string wsId, Stream binaryInput, CancellationToken cancellationToken)
        {
            if (!ConnectedWebSockets.TryGetValue(wsId, out var webSocket) || webSocket is not WebSocket)
            {
                throw new UnknownWebSocketIdException($"This WebSocket ({wsId}) is not recognized by the current " +
                                                      "application. Disconnecting...");
            }
            return webSocket.HandleBinaryInputInternal(binaryInput, cancellationToken);
        }

        public void RemoveWebSocket(string wsId) => ConnectedWebSockets.Remove(wsId);
    }
}