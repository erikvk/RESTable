using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Serialization;
using RESTar.WebSockets;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// An entity resource containing all the currently open WebSockets
    /// </summary>
    [RESTar(GET, DELETE)]
    public class WebSocket : ISelector<WebSocket>, IDeleter<WebSocket>
    {
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

        private RESTar.WebSocket _WebSocket { get; set; }

        /// <inheritdoc />
        public IEnumerable<WebSocket> Select(IQuery<WebSocket> query) => WebSocketController
            .AllSockets
            .Values
            .Select(socket => new WebSocket
            {
                Id = socket.TraceId,
                TerminalType = socket.TerminalResource?.Name,
                Client = JObject.Parse(Serializers.Json.Serialize(socket.GetConnectionProfile())),
                Terminal = JObject.Parse(Serializers.Json.Serialize(socket.Terminal)),
                _WebSocket = socket
            })
            .Where(query.Conditions);

        /// <inheritdoc />
        public int Delete(IQuery<WebSocket> query)
        {
            var count = 0;
            foreach (var entity in query.GetEntities())
            {
                entity._WebSocket.Disconnect();
                count += 1;
            }
            return count;
        }
    }
}