using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when entity validation failed for some resource
    /// </summary>
    public class FailedValidation : BadRequest
    {
        internal FailedValidation(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}