using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar is unable to get a response from a remote service request
    /// </summary>
    public class NoResponse : NotFound
    {
        internal NoResponse(string uri) : base(ErrorCodes.NoResponseFromRemoteService,
            "No response from remote service at " + uri)
        {
        }
    }
}