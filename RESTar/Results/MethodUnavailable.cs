using RESTar.Internal;
using RESTar.Resources;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request with an unavailable method
    /// search string.
    /// </summary>
    public class MethodUnavailable : Forbidden
    {
        /// <inheritdoc />
        public MethodUnavailable(Method method, IEntityResource resource) : base(ErrorCodes.NotAuthorized,
            $"{method} is not available for resource '{resource.Name}'") { }
    }
}