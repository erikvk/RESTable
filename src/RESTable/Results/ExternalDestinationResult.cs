using RESTable.Internal;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    internal class ExternalDestinationResult : Success
    {
        public sealed override IRequest Request { get; }

        internal ExternalDestinationResult(IRequest request, HttpResponse response) : base(request, response.Headers)
        {
            Request = request;
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
        }
    }
}