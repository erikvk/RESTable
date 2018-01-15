using RESTar.Internal;

namespace RESTar.Results.Fail.NotFound
{
    internal class NoHtml : NotFound
    {
        internal NoHtml(IEntityResource resource, string matcher) : base(ErrorCodes.NoMatchingHtml,
            $"No matching HTML file found for resource '{resource.FullName}'. Add a HTML file " +
            $"'{matcher}' to the 'wwwroot/resources' directory.") { }
    }
}