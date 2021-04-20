using System.Net;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Results
{
    /// <inheritdoc cref="RESTable.Results.Success" />
    /// <inheritdoc cref="RESTable.Results.IRequestResult" />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTable has taken over the 
    /// context from the network provider.
    /// </summary>
    public class WebSocketUpgradeSuccessful : Success
    {
        public WebSocket WebSocket { get; }

        public sealed override IRequest Request { get; }

        internal WebSocketUpgradeSuccessful(IRequest request, WebSocket webSocket) : base(request)
        {
            WebSocket = webSocket;
            Request = request;
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
        }
    }
}