using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot find an HTML representation file for a given resource
    /// </summary>
    public class NoHtml : NotFound
    {
        internal NoHtml(IEntityResource resource, string matcher) : base(ErrorCodes.NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") { }
    }
}