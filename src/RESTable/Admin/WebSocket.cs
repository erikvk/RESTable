﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.WebSockets;
using static RESTable.Method;

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <summary>
    /// An entity resource containing all the currently open WebSockets
    /// </summary>
    [RESTable(GET, DELETE, Description = description)]
    public class WebSocket : ISelector<WebSocket>, IAsyncDeleter<WebSocket>
    {
        private const string description = "Lists all connected WebSockets";

        /// <summary>
        /// The unique WebSocket ID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The type name of the terminal currently connected to the WebSocket 
        /// </summary>
        public string TerminalType { get; private set; }

        /// <summary>
        /// An object describing the terminal
        /// </summary>
        public JObject Terminal { get; private set; }

        /// <summary>
        /// An object describing the client
        /// </summary>
        public JObject Client { get; private set; }

        /// <summary>
        /// Does this WebSocket instance represent the currently connected client websocket?
        /// </summary>
        public bool IsThis { get; private set; }

        private WebSockets.WebSocket UnderlyingSocket { get; set; }

        /// <inheritdoc />
        public IEnumerable<WebSocket> Select(IRequest<WebSocket> request) => WebSocketController
            .AllSockets
            .Values
            .Select(socket => new WebSocket
            {
                Id = socket.TraceId,
                IsThis = socket.TraceId == request.Context.WebSocket?.TraceId,
                TerminalType = socket.TerminalResource?.Name,
                Client = JObject.FromObject(socket.GetAppProfile(), JsonProvider.Serializer),
                Terminal = socket.Terminal == null ? null : JObject.FromObject(socket.Terminal, JsonProvider.Serializer),
                UnderlyingSocket = socket
            });

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IRequest<WebSocket> request)
        {
            var count = 0;
            foreach (var entity in await request.GetInputEntities())
            {
                await entity.UnderlyingSocket.DisposeAsync();
                count += 1;
            }
            return count;
        }
    }
}