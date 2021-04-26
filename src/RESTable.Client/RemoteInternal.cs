namespace RESTable.Client
{
    /// <inheritdoc />
    /// <summary>
    /// An Internal error received from a remote RESTable service
    /// </summary>
    internal class RemoteInternal : Results.Internal
    {
        /// <inheritdoc />
        internal RemoteInternal(ErrorCodes code) : base(code, null) { }
    }
}   