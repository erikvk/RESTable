using System;
using System.Net;
using RESTar.Operations;
using RESTar.Queries;

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