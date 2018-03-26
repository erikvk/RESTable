using System;
using System.Net;
using RESTar.Queries;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when RESTar encounters a situation when it was asked to brew coffee while in 
    /// teapot mode.
    /// </summary>
    public class ImATeapot : Result
    {
        internal ImATeapot(ITraceable query) : base(query)
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