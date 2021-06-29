using System;
using System.Collections.Generic;
using RESTable.Resources.Operations;

namespace RESTable.Results
{
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