using RESTar.Http;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(HttpRequest request, string message) : base(ErrorCodes.InvalidSourceData,
            $"RESTar could not get entities from source at '{request.URI}'. {message}") { }
    }
}