using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request with an unavailable method
    /// search string.
    /// </summary>
    public class MethodUnavailable : Forbidden
    {
        /// <inheritdoc />
        public MethodUnavailable(Methods method, IEntityResource resource) : base(ErrorCodes.NotAuthorized,
            $"{method} is not available for resource '{resource.Name}'") { }
    }
}