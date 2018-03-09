using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be inserted because it's already in use
    /// </summary>
    public class AliasAlreadyInUse : BadRequest
    {
        internal AliasAlreadyInUse(Admin.ResourceAlias alias) : base(ErrorCodes.AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") { }
    }
}