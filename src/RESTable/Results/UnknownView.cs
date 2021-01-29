using RESTable.Meta;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot locate a view by some search string
    /// </summary>
    internal class UnknownView : NotFound
    {
        internal UnknownView(string searchString, ITarget resource) : base(ErrorCodes.UnknownResourceView,
            $"RESTable could not locate any resource view in '{resource.Name}' by '{searchString}'.") { }
    }
}