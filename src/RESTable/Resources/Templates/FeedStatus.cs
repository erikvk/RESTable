namespace RESTable.Resources.Templates
{
    /// <summary>
    /// The status of a feed terminal
    /// </summary>
    public enum FeedStatus
    {
        /// <summary>
        /// The Feed is connected to a WebSocket, but 
        /// currently marked as paused.
        /// </summary>
        PAUSED,

        /// <summary>
        /// The Feed is connected to a WebSocket and open,
        /// ready to receive messages.
        /// </summary>
        OPEN
    }
}