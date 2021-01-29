using System.Net;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// An unknown or other result received from a remote RESTable service
    /// </summary>
    internal class RemoteOther : Success
    {
        /// <inheritdoc />
        internal RemoteOther(ITraceable trace, HttpStatusCode statusCode, string statusDescription) : base(trace)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }
    }
}