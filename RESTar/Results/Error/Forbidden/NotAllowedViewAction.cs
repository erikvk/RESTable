using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <summary>
    /// Thrown when a clients tries to perform a forbidden action in the view
    /// </summary>
    internal class NotAllowedViewAction : Forbidden
    {
        internal NotAllowedViewAction(ErrorCodes code, string message) : base(code, message) { }
    }
}