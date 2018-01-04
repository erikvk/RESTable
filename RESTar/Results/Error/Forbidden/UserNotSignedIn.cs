using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class UserNotSignedIn : Forbidden
    {
        public UserNotSignedIn() : base(ErrorCodes.NotSignedIn, "User is not signed in") { }
    }
}
