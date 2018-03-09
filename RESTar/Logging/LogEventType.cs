namespace RESTar.Logging
{
    /// <summary>
    /// The log event types used when logging
    /// </summary>
    public enum LogEventType
    {
        /// <summary>
        /// Input in the form of a regular HTTP request
        /// </summary>
        HttpInput,

        /// <summary>
        /// Output in the form of a regular HTTP response
        /// </summary>
        HttpOutput,

        /// <summary>
        /// WebSocket input
        /// </summary>
        WebSocketInput,

        /// <summary>
        /// WebSocket output
        /// </summary>
        WebSocketOutput,

        /// <summary>
        /// A WebSocket was opened
        /// </summary>
        WebSocketOpen,

        /// <summary>
        /// A WebSocket was closed
        /// </summary>
        WebSocketClose
    }
}