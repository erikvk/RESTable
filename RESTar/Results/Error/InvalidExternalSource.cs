using RESTar.Http;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error getting entities from an external data source.
    /// </summary>
    public class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(HttpRequest request, string message) : base(ErrorCodes.InvalidSourceData,
            $"RESTar could not get entities from source at '{request.URI}'. {message}") { }
    }
}