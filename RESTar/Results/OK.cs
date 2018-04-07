using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    public abstract class OK : RequestSuccess
    {
        /// <inheritdoc />
        protected OK(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }

    internal class Valid : OK
    {
        public Valid(ITraceable trace) : base(trace) { }
    }
}