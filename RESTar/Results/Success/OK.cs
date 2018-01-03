using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    public class OK : Result
    {
        internal OK()
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}