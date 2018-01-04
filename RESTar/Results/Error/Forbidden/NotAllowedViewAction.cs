using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// Thrown when a clients tries to perform a forbidden action in the view
    /// </summary>
    public class NotAllowedViewAction : Forbidden.Base
    {
        internal NotAllowedViewAction(ErrorCodes code, string message) : base(code, message) { }
    }
}