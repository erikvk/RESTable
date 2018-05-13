namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : SerializedResultWrapper
    {
        internal WebSocketUpgradeFailed(Error error) : base(error) { }
    }
}