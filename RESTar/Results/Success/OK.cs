using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    internal class OK : Result
    {
        internal OK()
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}