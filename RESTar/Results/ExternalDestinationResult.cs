using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public class ExternalDestinationResult : Success
    {
        internal ExternalDestinationResult(IRequest request, HttpResponse response) : base(request)
        {
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
            Headers = response.Headers;
        }
    }
}