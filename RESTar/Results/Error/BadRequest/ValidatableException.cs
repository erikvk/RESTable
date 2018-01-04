using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an entity of a resource declared as IValidatable fails validation
    /// </summary>
    internal class ValidatableException : BadRequest
    {
        internal ValidatableException(string message) : base(ErrorCodes.InvalidResourceEntity, message) { }
    }
}