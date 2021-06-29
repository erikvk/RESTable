using System.Collections.Generic;
using System.Linq;
using RESTable.Resources.Operations;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when input entity validation failed for some resource
    /// </summary>
    public class InvalidInputEntity : BadRequest
    {
        public InvalidEntity InvalidEntity { get; }

        public InvalidInputEntity(InvalidEntity invalidEntity, string? info = null) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: info ?? "An invalid input entity was encountered"
        )
        {
            InvalidEntity = invalidEntity;
        }

        public InvalidInputEntity(IEnumerable<InvalidMember> invalidMembers, string? info = null) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: info ?? "An invalid input entity was encountered"
        )
        {
            InvalidEntity = new InvalidEntity(invalidMembers.ToList());
        }
    }
}