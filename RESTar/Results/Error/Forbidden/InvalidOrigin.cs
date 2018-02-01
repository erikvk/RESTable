using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class InvalidOrigin : Forbidden
    {
        internal InvalidOrigin() : base(ErrorCodes.NotAuthorized, "Invalid or unauthorized origin") { }
    }
}