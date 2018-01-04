using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class NotAuthorized : Base
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}