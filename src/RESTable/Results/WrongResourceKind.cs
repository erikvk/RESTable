using RESTable.Meta;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable cannot locate a resource of a certain kind by some search string, but found
///     a resource of a different kind.
/// </summary>
internal class WrongResourceKind : NotFound
{
    internal WrongResourceKind(string searchString, ResourceKind expected, ResourceKind found) : base(ErrorCodes.WrongResourceKind,
        $"RESTable expected to find a resource of kind {expected} by '{searchString}', but instead found a resource of kind {found}.") { }
}