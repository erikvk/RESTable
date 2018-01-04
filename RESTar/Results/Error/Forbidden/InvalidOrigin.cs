using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class InvalidOrigin : Forbidden
    {
        internal InvalidOrigin() : base(ErrorCodes.NotAuthorized, "Invalid or unauthorized origin") { }
    }
}