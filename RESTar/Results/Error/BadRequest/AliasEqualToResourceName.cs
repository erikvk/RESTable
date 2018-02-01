using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class AliasEqualToResourceName : BadRequest
    {
        internal AliasEqualToResourceName(string alias) : base(ErrorCodes.AliasEqualToResourceName,
            $"Invalid Alias: '{alias}' is a resource name") { }
    }
}