using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    public class AliasAlreadyInUse : BadRequest
    {
        internal AliasAlreadyInUse(Admin.ResourceAlias alias) : base(ErrorCodes.AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") { }
    }
}