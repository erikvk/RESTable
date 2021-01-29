using RESTable.Meta;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an alias cannot be inserted because RESTable cannot locate a resource by some
    /// search string.
    /// </summary>
    internal class UnknownResourceForAlias : BadRequest
    {
        internal UnknownResourceForAlias(string searchString, IResource match) : base(ErrorCodes.UnknownResource,
            "Resource alias assignments must be provided with fully qualified resource names. No match " +
            $"for '{searchString}'. {(match != null ? $"Did you mean '{match.Name}'? " : "")}") { }
    }
}