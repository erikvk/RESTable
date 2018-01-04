using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <summary>
    /// Throw when a request for an unsupported OData protocol version was encountered
    /// </summary>
    internal class UnsupportedODataVersion : BadRequest
    {
        internal UnsupportedODataVersion() : base(ErrorCodes.UnsupportedODataProtocolVersion,
            "Unsupported OData protocol version. Supported protocol version: 4.0") { }
    }
}