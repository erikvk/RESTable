using System.Net;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar
{
    internal interface IWebSocketInternal : IWebSocket
    {
        void Open();
        void SendQueuedMessages();
        void SetFallbackHandlers(WebSocketReceiveAction receiveAction, WebSocketDisconnectAction disconnectAction);
        void HandleInput(string input);
        void HandleDisconnect();
        void SetCurrentLocation(string location);
        IPAddress ClientIpAddress { get; }
        ITarget Target { get; set; }
    }
}