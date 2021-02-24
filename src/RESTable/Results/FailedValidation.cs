using RESTable.Resources.Operations;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when entity validation failed for some resource
    /// </summary>
    public class FailedValidation : BadRequest
    {
        public InvalidEntity InvalidEntity { get; }

        internal FailedValidation(InvalidEntity invalidEntity) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: "Entity validation failed"
        )
        {
            InvalidEntity = invalidEntity;
        }
    }
}