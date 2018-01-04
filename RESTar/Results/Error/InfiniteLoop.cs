using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar encounters an infinite loop when evaluating a request
    /// </summary>
    internal class InfiniteLoop : RESTarException
    {
        internal InfiniteLoop() : base(ErrorCodes.InfiniteLoopDetected,
            "RESTar encountered a potentially infinite loop of recursive internal calls.")
        {
            StatusCode = (HttpStatusCode) 508;
            StatusDescription = "Infinite loop detected";
        }

        internal InfiniteLoop(string message) : base(ErrorCodes.InfiniteLoopDetected, message)
        {
            StatusCode = (HttpStatusCode) 508;
            StatusDescription = "Infinite loop detected";
        }
    }
}