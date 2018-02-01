using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class FailedValidation : BadRequest
    {
        internal FailedValidation(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}