using System;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    public abstract class Success : Result
    {
        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        protected Success(ITraceable trace) : base(trace) { }
    }
}