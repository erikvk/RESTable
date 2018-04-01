using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be inserted because it's equal to some resource name
    /// </summary>
    public class AliasEqualToResourceName : Error
    {
        internal AliasEqualToResourceName(ITraceable trace, string alias) : base(trace, ErrorCodes.AliasEqualToResourceName,
            $"Invalid Alias: '{alias}' is a resource name") { }
    }
}