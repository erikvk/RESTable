using RESTar.Operations;

namespace RESTar
{
    internal interface IWebSocketInternal
    {
        void Open();
        void SendQueuedMessages();
        void SetFallbackHandlers(WebSocketReceiveAction receiveAction, WebSocketDisconnectAction disconnectAction);
        void HandleInput(string input);
        void HandleDisconnect();
    }
}