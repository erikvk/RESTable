using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <summary>
    /// Thrown when RESTar cannot locate a macro based on some search string
    /// </summary>
    public class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") { }
    }
}