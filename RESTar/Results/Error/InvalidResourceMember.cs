using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidResourceMember : Error.BadRequest.BadRequest
    {
        internal InvalidResourceMember(string message) : base(ErrorCodes.InvalidResourceMember, message) { }
    }
}