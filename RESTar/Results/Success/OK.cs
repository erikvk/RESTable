using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    public abstract class OK : Result
    {
        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        protected OK(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
        }
    }
}