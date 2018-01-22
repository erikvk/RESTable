using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.WebSockets;
using static RESTar.Methods;

namespace RESTar.Admin
{
    [RESTar(GET, DELETE)]
    internal class WebSocket : ISelector<WebSocket>, IDeleter<WebSocket>
    {
        public string Id { get; private set; }
        public string TerminalType { get; private set; }
        public ITerminal Terminal { get; private set; }
        public ClientProfile Client { get; private set; }
        private IWebSocketInternal WebSocketInternal { get; set; }

        public IEnumerable<WebSocket> Select(IRequest<WebSocket> request) => WebSocketController
            .AllSockets
            .Values
            .Select(socket => new WebSocket
            {
                Id = socket.TraceId,
                TerminalType = socket.TerminalResource.Name,
                Client = socket.GetClientProfile(),
                Terminal = socket.Terminal,
                WebSocketInternal = socket
            })
            .Where(request.Conditions);

        public int Delete(IEnumerable<WebSocket> entities, IRequest<WebSocket> request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                entity.WebSocketInternal.Disconnect();
                count += 1;
            }
            return count;
        }
    }
}