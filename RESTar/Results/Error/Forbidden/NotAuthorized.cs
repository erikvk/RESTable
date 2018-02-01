using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class NotAuthorized : Forbidden
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}