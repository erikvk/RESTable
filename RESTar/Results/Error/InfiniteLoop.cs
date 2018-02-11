using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an infinite loop of recursive internal requests
    /// </summary>
    public class InfiniteLoop : RESTarError
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