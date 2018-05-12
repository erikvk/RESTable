namespace RESTar.Meta
{
    /// <summary>
    /// The kinds of resources in RESTar
    /// </summary>
    public enum ResourceKind
    {
        /// <summary>
        /// Holds a set of entities that can be manipulated using REST methods like GET and POST
        /// </summary>
        EntityResource,

        /// <summary>
        /// Small interactive applications that can be run over a WebSocket
        /// </summary>
        TerminalResource,

        /// <summary>
        /// Holds raw binary data
        /// </summary>
        BinaryResource
    }
}