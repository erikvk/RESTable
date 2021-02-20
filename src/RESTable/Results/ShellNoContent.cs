using System.Net;

namespace RESTable.Results
{
    internal class ShellNoContent : ShellSuccess
    {
        internal ShellNoContent(IProtocolHolder protocolHolder) : base(protocolHolder)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
        }
    }
}