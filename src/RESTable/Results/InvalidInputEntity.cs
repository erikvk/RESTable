using System;
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

        internal InvalidInputEntity(InvalidEntity invalidEntity, string info = null) : base
        (
            code: ErrorCodes.InvalidResourceEntity,
            info: info ?? "An invalid input entity was encountered"
        )
        {
            InvalidEntity = invalidEntity;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when a call to a terminal constructor failed due do missing or invalid parameters
    /// </summary>
    public class MissingTerminalParameter : InvalidInputEntity
    {
        internal MissingTerminalParameter(Type terminalType, InvalidEntity invalidEntity) : base
        (
            invalidEntity: invalidEntity,
            info: $"Missing or invalid terminal parameters in request to '{terminalType.GetRESTableTypeName()}'"
        ) { }
    }
}