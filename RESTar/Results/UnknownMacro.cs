using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro based on some search string
    /// </summary>
    internal class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") { }
    }
}