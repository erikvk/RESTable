using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be inserted because it's already in use
    /// </summary>
    public class AliasAlreadyInUse : Error
    {
        internal AliasAlreadyInUse(ITraceable trace, Admin.ResourceAlias alias) : base(trace, ErrorCodes.AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") { }
    }
}