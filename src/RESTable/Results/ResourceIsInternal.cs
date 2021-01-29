using RESTable.Meta;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an external request for an internal resource
    /// search string.
    /// </summary>
    internal class ResourceIsInternal : Forbidden
    {
        internal ResourceIsInternal(IResource resource) : base(ErrorCodes.ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") { }
    }
}