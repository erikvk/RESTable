using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot get entities from an external source
    /// </summary>
    public class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(HttpRequest request, string message) : base(ErrorCodes.InvalidSourceData,
            $"RESTar could not get entities from source at '{request.URI}'. {message}") { }
    }
}