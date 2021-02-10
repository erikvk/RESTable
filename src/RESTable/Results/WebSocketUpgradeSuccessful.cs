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

        internal WebSocketUpgradeSuccessful(IRequest request, WebSocket webSocket) : base(request)
        {
            WebSocket = webSocket;
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
            TimeElapsed = request.TimeElapsed;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTable has already sent
    /// the response back and closed the socket.
    /// </summary>
    public class WebSocketTransferSuccess : OK
    {
        internal WebSocketTransferSuccess(IRequest request) : base(request) { }
    }
}