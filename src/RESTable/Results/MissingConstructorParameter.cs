using System;
using System.Collections.Generic;
using RESTable.Resources.Operations;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when a call to an entity constructor failed due do missing or invalid parameters
/// </summary>
public class MissingConstructorParameter : InvalidInputEntity
{
    internal MissingConstructorParameter(Type entityType, IEnumerable<InvalidMember> invalidMembers) : base
    (
        invalidMembers,
        $"Missing non-optional constructor parameters for type '{entityType.GetRESTableTypeName()}'"
    ) { }
}