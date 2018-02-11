using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class NotAcceptable : RESTarError
    {
        public NotAcceptable(MimeType unsupported) : base(ErrorCodes.NotAcceptable,
            $"Unsupported accept format: '{unsupported.TypeCodeString}'")
        {
            StatusCode = HttpStatusCode.NotAcceptable;
            StatusDescription = "Not acceptable";
        }
    }
}