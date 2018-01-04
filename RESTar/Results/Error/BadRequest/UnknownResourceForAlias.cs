using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class UnknownResourceForAlias : BadRequest
    {
        internal UnknownResourceForAlias(string searchString, IResource match) : base(ErrorCodes.UnknownResource,
            "Resource alias assignments must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.Name}'? " : "")}") { }
    }
}