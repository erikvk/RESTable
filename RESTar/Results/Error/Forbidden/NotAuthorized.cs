using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class NotAuthorized : Forbidden
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}