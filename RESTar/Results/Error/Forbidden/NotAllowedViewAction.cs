using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class NotAllowedViewAction : Forbidden
    {
        internal NotAllowedViewAction(ErrorCodes code, string message) : base(code, message) { }
    }
}