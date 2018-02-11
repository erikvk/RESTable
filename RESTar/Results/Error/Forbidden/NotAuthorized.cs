using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unauthorized access attempt
    /// search string.
    /// </summary>
    public class NotAuthorized : Forbidden
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}