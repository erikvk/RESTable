using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar receives a request from an invalid or unauthorized origin
    /// search string.
    /// </summary>
    internal class InvalidOrigin : Forbidden
    {
        internal InvalidOrigin() : base(ErrorCodes.NotAuthorized, "Invalid or unauthorized origin") { }
    }
}