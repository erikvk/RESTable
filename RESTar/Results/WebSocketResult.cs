using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    internal class SwitchedTerminal : Success
    {
        internal SwitchedTerminal(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "Switched terminal";
            TimeElapsed = request.TimeElapsed;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : Error
    {
        internal WebSocketUpgradeFailed(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTar has taken over the 
    /// context from the network provider.
    /// </summary>
    public class WebSocketUpgradeSuccessful : Success
    {
        internal WebSocketUpgradeSuccessful(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
            TimeElapsed = request.TimeElapsed;
        }
    }
}