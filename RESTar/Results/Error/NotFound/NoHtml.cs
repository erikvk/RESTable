using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    public class NoHtml : NotFound
    {
        internal NoHtml(IEntityResource resource, string matcher) : base(ErrorCodes.NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.Name}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") { }
    }
}