using System.Net;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// An unknown or other result received from a remote RESTable service
    /// </summary>
    internal sealed class RemoteOther : Success
    {
        /// <inheritdoc />
        internal RemoteOther(IProtocolHolder protocolHolder, HttpStatusCode statusCode, string statusDescription) : base(protocolHolder)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }
    }
}