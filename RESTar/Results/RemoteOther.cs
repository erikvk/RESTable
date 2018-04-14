using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// An unknown or other result received from a remote RESTar service
    /// </summary>
    public class RemoteOther : Success
    {
        /// <inheritdoc />
        internal RemoteOther(ITraceable trace, HttpStatusCode statusCode, string statusDescription) : base(trace)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }
    }
}