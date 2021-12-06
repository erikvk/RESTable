using System.Collections.Generic;
using System.Linq;
using RESTable.Resources.Operations;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when input entity validation failed for some resource
/// </summary>
public class InvalidInputEntity : BadRequest
{
    public InvalidInputEntity(InvalidEntity invalidEntity, string? info = null) : base
    (
        ErrorCodes.InvalidResourceEntity,
        info ?? "An invalid input entity was encountered"
    )
    {
        InvalidEntity = invalidEntity;
    }

    public InvalidInputEntity(IEnumerable<InvalidMember> invalidMembers, string? info = null) : base
    (
        ErrorCodes.InvalidResourceEntity,
        info ?? "An invalid input entity was encountered"
    )
    {
        InvalidEntity = new InvalidEntity(invalidMembers.ToList());
    }

    public InvalidEntity InvalidEntity { get; }
}