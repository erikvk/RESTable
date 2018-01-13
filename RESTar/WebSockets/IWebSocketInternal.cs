using System.Net;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket
    {
        void Open();
        void SetShellHandler(WebSocketReceiveAction shellHandler);
        void HandleInput(string input);
        void HandleDisconnect();
        void SetCurrentLocation(string location);
        IPAddress ClientIpAddress { get; }
        ITarget Target { get; set; }
        bool IsShell { get; }
        string CurrentLocation { get; }
        void SetQueryProperties(string query, Headers headers, TCPConnection connection);
    }
}