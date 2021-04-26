using System.Net;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Client
{
    /// <inheritdoc />
    /// <summary>
    /// An unknown or other result received from a remote RESTable service
    /// </summary>
    internal sealed class RemoteOther : Success
    {
        public override IRequest Request { get; }

        /// <inheritdoc />
        internal RemoteOther(IProtocolHolder protocolHolder, HttpStatusCode statusCode, string statusDescription) : base(protocolHolder)
        {
            Request = null;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }
    }
}