using System.Net;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned when RESTable encounters a situation when it was asked to brew coffee while in teapot mode.
    /// </summary>
    internal class ImATeapot : RequestSuccess
    {
        internal ImATeapot(IRequest request) : base(request)
        {
            StatusCode = (HttpStatusCode) 418;
            StatusDescription = "I'm a teapot";
            Headers.Info = "Look at what you did... You tried something real hacky, and now RESTable " +
                           "thinks it's in some kind of... teapot mode? Hope you're very proud of " +
                           "yourself...";
        }
    }
}