using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class OK : Result
    {
        internal OK(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}