using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an action in the view, that was not allowed for the given resource or user
    /// </summary>
    public class NotAllowedViewAction : Forbidden
    {
        internal NotAllowedViewAction(ErrorCodes code, string message) : base(code, message) { }
    }
}