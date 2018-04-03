using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be inserted because it's equal to some resource name
    /// </summary>
    public class AliasEqualToResourceName : Error
    {
        internal AliasEqualToResourceName(string alias) : base(ErrorCodes.AliasEqualToResourceName, $"Invalid Alias: '{alias}' is a resource name") { }
    }
}