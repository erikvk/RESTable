using System.Collections.Generic;
using System.IO;
using RESTar.Operations;
using RESTar.Results.Success;

namespace RESTar.Requests
{
    /// <summary>
    /// A wrapper for a WebSocket, exposed to the resource type.
    /// </summary>
    internal class WebSocketWrapper<T> : IWebSocket<T> where T : class
    {
        private readonly IWebSocket WebSocket;
        public IRequest<T> Request { get; }

        #region Forwarders

        public WebSocketReceiveAction InputHandler
        {
            set => WebSocket.InputHandler = value;
        }

        public WebSocketDisconnectAction DisconnectHandler
        {
            set => WebSocket.DisconnectHandler = value;
        }

        public string Id => WebSocket.Id;
        public void Send(string data) => WebSocket.Send(data);
        public void Send(Stream data) => WebSocket.Send(data);
        public void Disconnect() => WebSocket.Disconnect();
        public WebSocketStatus Status => WebSocket.Status;
        public string CurrentLocation => WebSocket.CurrentLocation;

        #endregion

        public void Send(T entity)
        {
            if (entity == null) return;
            var result = Entities.Create(Request, new[] {entity});
            var finalized = RESTarProtocolProvider.FinalizeResult(result);
            Send(finalized.Body);
        }

        public void Send(IEnumerable<T> entities)
        {
            if (entities == null) return;
            var result = Entities.Create(Request, entities);
            var finalized = RESTarProtocolProvider.FinalizeResult(result);
            Send(finalized.Body);
        }

        public WebSocketWrapper(IRequest<T> request, IWebSocket webSocket)
        {
            Request = request;
            WebSocket = webSocket;
        }
    }
}