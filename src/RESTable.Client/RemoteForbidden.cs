using RESTable.Results;

namespace RESTable.Client
{
    /// <inheritdoc />
    /// <summary>
    /// A Forbidden result received from a remote RESTable service 
    /// </summary>
    public class RemoteForbidden : Forbidden
    {
        /// <inheritdoc />
        internal RemoteForbidden(ErrorCodes code) : base(code, null) { }
    }
}