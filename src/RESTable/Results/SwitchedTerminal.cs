using System.Net;
using RESTable.Requests;

namespace RESTable.Results
{
    internal class SwitchedTerminal : Success
    {
        internal SwitchedTerminal(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "Switched terminal";
            TimeElapsed = request.TimeElapsed;
        }
    }
}