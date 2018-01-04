using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class ValidatableException : BadRequest
    {
        internal ValidatableException(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}