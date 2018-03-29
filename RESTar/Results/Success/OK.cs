using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    public abstract class OK : Success
    {
        /// <inheritdoc />
        protected OK(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}