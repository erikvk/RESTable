using System;
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

        public InvalidInputEntity(InvalidEntity invalidEntity, string info = null) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: info ?? "An invalid input entity was encountered"
        )
        {
            InvalidEntity = invalidEntity;
        }

        public InvalidInputEntity(IEnumerable<InvalidMember> invalidMembers, string info = null) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: info ?? "An invalid input entity was encountered"
        )
        {
            InvalidEntity = new InvalidEntity(invalidMembers.ToList());
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a call to a terminal constructor failed due do missing or invalid parameters
    /// </summary>
    public class MissingTerminalParameter : InvalidInputEntity
    {
        internal MissingTerminalParameter(Type terminalType, IEnumerable<InvalidMember> invalidMembers) : base
        (
            invalidMembers: invalidMembers,
            info: $"Missing or invalid terminal parameters in request to '{terminalType.GetRESTableTypeName()}'"
        ) { }
    }
}