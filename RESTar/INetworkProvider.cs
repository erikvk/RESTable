namespace RESTar
{
    /// <summary>
    /// Specifies the operations of a RESTar network provider
    /// </summary>
    public interface INetworkProvider
    {
        /// <summary>
        /// Adds HTTP request bindings for the given methods on the given port and root URI
        /// </summary>
        void AddBindings(Method[] methods, string rootUri, ushort port);

        /// <summary>
        /// Removes HTTP request bindings for the given methods on the given port and root URI
        /// </summary>
        void RemoveBindings(Method[] methods, string rootUri, ushort port);
    }
}