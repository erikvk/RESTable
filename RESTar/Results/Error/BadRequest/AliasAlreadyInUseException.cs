using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is already in use
    /// </summary>
    internal class AliasAlreadyInUseException : BadRequest
    {
        internal AliasAlreadyInUseException(Admin.ResourceAlias alias) : base(ErrorCodes.AliasAlreadyInUse,
            $"Invalid Alias: '{alias.Alias}' is already in use for resource '{alias.IResource.Name}'") { }
    }
}