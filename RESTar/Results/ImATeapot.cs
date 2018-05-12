using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when RESTar encounters a situation when it was asked to brew coffee while in 
    /// teapot mode.
    /// </summary>
    public class ImATeapot : RequestSuccess
    {
        internal ImATeapot(IRequest request) : base(request)
        {
            StatusCode = (HttpStatusCode) 418;
            StatusDescription = "I'm a teapot";
            Headers.Info = "Look what you did. You tried something real hacky, and now RESTar " +
                           "thinks it's in some kind of 'teapot mode'. I hope you're proud of " +
                           "yourself...";
        }
    }
}