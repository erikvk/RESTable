namespace RESTar
{
    /// <summary>
    /// The status of a RESTar WebSocket connection
    /// </summary>
    public enum WebSocketStatus
    {
        /// <summary>
        /// The WebSocket is closed. No messages can be sent or received
        /// </summary>
        Closed,

        /// <summary>
        /// The WebSocket is open. Messages can be sent and received.
        /// </summary>
        Open,

        /// <summary>
        /// The WebSocket is pending and will soon open. Sent messages will be queued until the WebSocket
        /// is opened. Messages cannot be received.
        /// </summary>
        PendingOpen,

        /// <summary>
        /// The WebSocket is pending and will soon close. Messages cannot be sent. Disconnect calls are
        /// ignored.
        /// </summary>
        PendingClose
    }
}