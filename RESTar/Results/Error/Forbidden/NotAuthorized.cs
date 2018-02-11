using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public class NotAuthorized : Forbidden
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}