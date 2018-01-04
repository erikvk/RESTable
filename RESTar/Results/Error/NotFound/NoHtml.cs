using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when no HTML was found for a resource view.
    /// </summary>
    public class NoHtml : NotFound
    {
        internal NoHtml(IResource resource, string matcher) : base(ErrorCodes.NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") { }
    }
}