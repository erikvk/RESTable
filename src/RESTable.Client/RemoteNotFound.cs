using RESTable.Results;

namespace RESTable.Client
{
    /// <inheritdoc />
    /// <summary>
    /// A Not Found result received from a remote RESTable service
    /// </summary>
    internal class RemoteNotFound : NotFound
    {
        /// <inheritdoc />
        internal RemoteNotFound(ErrorCodes code) : base(code, null) { }
    }
}