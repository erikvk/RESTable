using System;
using System.Net;

namespace RESTable.Results
{
    internal class ShellNoContent : ShellSuccess
    {
        internal ShellNoContent(IProtocolHolder protocolHolder, TimeSpan elapsed) : base(protocolHolder)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            TimeElapsed = elapsed;
        }
    }
}