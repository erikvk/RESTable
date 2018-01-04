using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class UnsupportedODataVersion : BadRequest
    {
        internal UnsupportedODataVersion() : base(ErrorCodes.UnsupportedODataProtocolVersion,
            "Unsupported OData protocol version. Supported protocol version: 4.0") { }
    }
}