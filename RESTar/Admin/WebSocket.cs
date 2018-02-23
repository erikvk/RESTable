using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Serialization;
using RESTar.WebSockets;
using static RESTar.Methods;

namespace RESTar.Admin
{
    [RESTar(GET, DELETE)]
    internal class WebSocket : ISelector<WebSocket>, IDeleter<WebSocket>
    {
        public string Id { get; private set; }
        public string TerminalType { get; private set; }
        public JObject Terminal { get; private set; }
        public JObject Client { get; private set; }
        private IWebSocketInternal WebSocketInternal { get; set; }

        public IEnumerable<WebSocket> Select(IRequest<WebSocket> request) => WebSocketController
            .AllSockets
            .Values
            .Select(socket => new WebSocket
            {
                Id = socket.TraceId,
                TerminalType = socket.TerminalResource.Name,
                Client = JObject.Parse(Serializers.Json.Serialize(socket.GetConnectionProfile())),
                Terminal = JObject.Parse(Serializers.Json.Serialize(socket.Terminal)),
                WebSocketInternal = socket
            })
            .Where(request.Conditions);

        public int Delete(IRequest<WebSocket> request)
        {
            var count = 0;
            foreach (var entity in request.GetEntities())
            {
                entity.WebSocketInternal.Disconnect();
                count += 1;
            }
            return count;
        }
    }
}