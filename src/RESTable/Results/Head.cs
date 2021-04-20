using System.Net;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; }

        public Head(IRequest request, ulong count) : base(request)
        {
            EntityCount = count;
            if (count > 0) return;
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers.Info = "No entities found matching request.";
            if (Request.Headers.Metadata == "full")
                Headers.Metadata = Metadata;
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Head)};{Request.Resource};{EntityCount}";
    }
}