using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be registered for a resource because it is the same as a resource name
    /// </summary>
    public class AliasEqualToResourceNameException : Base
    {
        internal AliasEqualToResourceNameException(string alias) : base(ErrorCodes.AliasEqualToResourceName,
            $"Invalid Alias: '{alias}' is a resource name") { }
    }
}