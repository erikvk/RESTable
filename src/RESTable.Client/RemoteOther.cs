using System.Net;
using RESTable.Results;

namespace RESTable.Client
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