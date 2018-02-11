using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    public class FailedValidation : BadRequest
    {
        internal FailedValidation(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}