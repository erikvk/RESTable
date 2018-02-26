namespace RESTar.Requests {
    /// <summary>
    /// Defines something that can be traced from a TCP connection
    /// </summary>
    public interface ITraceable
    {
        /// <summary>
        /// A unique ID
        /// </summary>
        string TraceId { get; }

        /// <summary>
        /// The initial TCP connection
        /// </summary>
        TCPConnection TcpConnection { get; }
    }
}