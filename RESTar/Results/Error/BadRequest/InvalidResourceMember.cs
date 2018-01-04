using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class InvalidResourceMember : BadRequest
    {
        internal InvalidResourceMember(string message) : base(ErrorCodes.InvalidResourceMember, message) { }
    }
}