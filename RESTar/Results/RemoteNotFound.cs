using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A Not Found result received from a remote RESTar service
    /// </summary>
    public class RemoteNotFound : NotFound
    {
        /// <inheritdoc />
        internal RemoteNotFound(ErrorCodes code) : base(code, null) { }
    }
}