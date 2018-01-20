using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class NoContent : Result
    {
        internal NoContent(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers["RESTar-info"] = "No entities found matching request.";
        }
    }
}