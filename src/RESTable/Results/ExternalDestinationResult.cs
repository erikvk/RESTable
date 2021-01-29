using RESTable.Internal;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    internal class ExternalDestinationResult : Success
    {
        /// <inheritdoc />
        public override Headers Headers { get; }

        internal ExternalDestinationResult(IRequest request, HttpResponse response) : base(request)
        {
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
            Headers = response.Headers;
        }
    }
}