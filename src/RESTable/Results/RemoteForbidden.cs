namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A Forbidden result received from a remote RESTable service 
    /// </summary>
    internal class RemoteForbidden : Forbidden
    {
        /// <inheritdoc />
        internal RemoteForbidden(ErrorCodes code) : base(code, null) { }
    }
}