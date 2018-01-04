using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class FailedValidation : BadRequest
    {
        internal FailedValidation(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}