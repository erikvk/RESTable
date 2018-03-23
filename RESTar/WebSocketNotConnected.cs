using RESTar.Internal;
using RESTar.Results.Error;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Throw when RESTar encounters an attempt to interact with a closed WebSocket connection
    /// </summary>
    public class WebSocketNotConnected : RESTarError
    {
        internal WebSocketNotConnected() : base(ErrorCodes.WebSocketNotConnected,
            "An attempt was made to interact with a closed WebSocket connection") { }
    }
}