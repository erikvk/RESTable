using System.Net;

namespace RESTable.Results
{
    internal class ShellNoQuery : ShellSuccess
    {
        internal ShellNoQuery(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = default;
        }
    }
}