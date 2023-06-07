using System;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable cannot locate a property by some search string
/// </summary>
internal class UnknownProperty : NotFound
{
    internal UnknownProperty(Type targetType, IResource? resource, string searchString) : base
    (
        ErrorCodes.UnknownProperty,
        $"Could not find any property in {(targetType.HasAttribute<RESTableViewAttribute>() ? $"view '{targetType.Name}' or type '{resource?.Name}'" : $"type '{targetType.Name}'")} by '{searchString}'.") { }
}
