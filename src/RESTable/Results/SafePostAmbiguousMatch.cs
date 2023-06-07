namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when a uniquely matched entity was expected for a request, but multiple was found
/// </summary>
internal class SafePostAmbiguousMatch : BadRequest
{
    internal SafePostAmbiguousMatch(int count, string uri) : base(ErrorCodes.AmbiguousMatch,
        $"As part of the SafePost operation, RESTable tried to select entities using '{uri}', expecting either no " +
        $"or a single entity as result. Found {count} entities matching this URI. SafePost, like PUT, can only be " +
        "used on entity resources where zero or exacly one entity is selected by the input conditions.") { }
}
