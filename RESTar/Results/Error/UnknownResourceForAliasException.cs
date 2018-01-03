using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a resource cannot be identified when registering an alias.
    /// </summary>
    public class UnknownResourceForAliasException : BadRequest
    {
        internal UnknownResourceForAliasException(string searchString, IResource match) : base(ErrorCodes.UnknownResource,
            "Resource alias assignments must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.Name}'? " : "")}") { }
    }
}