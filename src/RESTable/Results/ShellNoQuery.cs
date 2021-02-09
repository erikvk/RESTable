using System.Net;

namespace RESTable.Results
{
    internal class ShellNoQuery : ShellSuccess
    {
        internal ShellNoQuery(IProtocolHolder protocolHolder) : base(protocolHolder)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = default;
        }
    }
}