using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    internal class NoContent : Result
    {
        internal NoContent()
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers["RESTar-info"] = "No entities found matching request.";
        }
    }
}