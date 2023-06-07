namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable cannot uniquely identify a resource by some search string
/// </summary>
internal class AmbiguousResource : NotFound
{
    internal AmbiguousResource(string searchString) : base(ErrorCodes.AmbiguousResource,
        $"RESTable could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
}
