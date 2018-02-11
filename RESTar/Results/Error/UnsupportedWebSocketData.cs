using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class UnsupportedWebSocketInput : RESTarError
    {
        internal UnsupportedWebSocketInput(string message) : base(ErrorCodes.UnsupportedContent, message)
        {
            StatusCode = HttpStatusCode.UnsupportedMediaType;
            StatusDescription = "Unsupported media type";
        }
    }
}