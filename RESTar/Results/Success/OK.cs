using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    public abstract class OK : Result
    {
        /// <inheritdoc />
        protected OK(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}