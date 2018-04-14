using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A Forbidden result received from a remote RESTar service 
    /// </summary>
    public class RemoteForbidden : Forbidden
    {
        /// <inheritdoc />
        internal RemoteForbidden(ErrorCodes code) : base(code, null) { }
    }
}