namespace RESTar.Requests
{
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
        /// Client to which this trace can be led
        /// </summary>
        Client Client { get; }
    }
}