using RESTar.Internal;

namespace RESTar.Results.Fail.NotFound
{
    internal class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any macro by '{searchString}'.") { }
    }
}