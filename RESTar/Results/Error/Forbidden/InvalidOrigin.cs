using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public class InvalidOrigin : Forbidden
    {
        internal InvalidOrigin() : base(ErrorCodes.NotAuthorized, "Invalid or unauthorized origin") { }
    }
}