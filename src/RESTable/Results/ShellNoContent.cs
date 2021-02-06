using System;
using System.Net;

namespace RESTable.Results
{
    internal class ShellNoContent : ShellSuccess
    {
        internal ShellNoContent(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            TimeElapsed = elapsed;
        }
    }
}