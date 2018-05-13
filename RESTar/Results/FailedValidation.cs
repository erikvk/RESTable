using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when entity validation failed for some resource
    /// </summary>
    internal class FailedValidation : BadRequest
    {
        internal FailedValidation(string info) : base(ErrorCodes.InvalidResourceEntity, info) { }
    }
}