using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when the current view user is not logged in
    /// </summary>
    public class UserNotSignedIn : Forbidden
    {
        /// <inheritdoc />
        public UserNotSignedIn() : base(ErrorCodes.NotSignedIn, "User is not signed in") { }
    }
}
