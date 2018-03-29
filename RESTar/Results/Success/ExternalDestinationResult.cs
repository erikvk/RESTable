using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    public class ExternalDestinationResult : Success
    {
        internal ExternalDestinationResult(ITraceable trace, HttpResponse response) : base(trace)
        {
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
            Headers = response.Headers;
        }
    }
}