using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro using a given search string
    /// </summary>
    public class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") { }
    }
}