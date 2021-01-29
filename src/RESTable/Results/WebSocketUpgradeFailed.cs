namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : ResultWrapper
    {
        internal WebSocketUpgradeFailed(Error error) : base(error) { }
    }
}