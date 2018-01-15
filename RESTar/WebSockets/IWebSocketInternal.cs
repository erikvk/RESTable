using System.Net;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket
    {
        void Open();
        void HandleDisconnect();
        void SetCurrentLocation(string location);
        IPAddress ClientIpAddress { get; }
        ITarget Target { get; set; }
        bool IsShell { get; }
        ITerminal Terminal { get; set; }
        string CurrentLocation { get; }
        void SetQueryProperties(string query, Headers headers, TCPConnection connection);
    }
}