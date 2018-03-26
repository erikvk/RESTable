using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when RESTar encounters a situation when it was asked to brew coffee while in 
    /// teapot mode.
    /// </summary>
    public class ImATeapot : Result
    {
        internal ImATeapot(ITraceable trace) : base(trace)
        {
            StatusCode = (HttpStatusCode) 418;
            StatusDescription = "I'm a teapot";
            Headers["RESTar-info"] = "Look what you did. You tried something real hacky, and now RESTar " +
                                     "thinks it's in some kind of 'teapot mode'. I hope you're proud of " +
                                     "yourself...";
            TimeElapsed = default;
        }

        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }
    }
}