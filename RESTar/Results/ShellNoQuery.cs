using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    internal class ShellNoQuery : Success
    {
        internal ShellNoQuery(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = default;
        }
    }
}