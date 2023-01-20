using System.Text.RegularExpressions;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable cannot locate a resource by some search string
/// </summary>
public partial class UnknownResource : NotFound
{
    /// <inheritdoc />
    public UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
        $"RESTable could not locate any resource by '{searchString}'." +
        (ResourceRegex().IsMatch(searchString) ? " Check request URI syntax." : "")) { }

    [GeneratedRegex("[^\\w\\d\\.]+")]
    private static partial Regex ResourceRegex();
}
