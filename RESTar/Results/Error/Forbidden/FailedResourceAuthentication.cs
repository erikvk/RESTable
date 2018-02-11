using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a request failed resource-specific authentication
    /// search string.
    /// </summary>
    public class FailedResourceAuthentication : Forbidden
    {
        /// <inheritdoc />
        internal FailedResourceAuthentication(string message) : base(ErrorCodes.FailedResourceAuthentication, message) { }
    }
}