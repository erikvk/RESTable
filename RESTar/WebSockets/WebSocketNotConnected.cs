using RESTar.Internal;
using RESTar.Results;

namespace RESTar.WebSockets
{
    /// <inheritdoc />
    /// <summary>
    /// Throw when RESTar encounters an attempt to interact with a closed WebSocket connection
    /// </summary>
    public class WebSocketNotConnected : Error
    {
        internal WebSocketNotConnected() : base(ErrorCodes.WebSocketNotConnected,
            "An attempt was made to interact with a closed WebSocket connection") { }
    }
}