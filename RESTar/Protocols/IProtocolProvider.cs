using System.Collections.Generic;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Protocols
{
    internal interface IProtocolProvider
    {
        Arguments MakeRequestArguments(string uri, byte[] body = null, IDictionary<string, string> headers = null,
            string contentType = null, string accept = null);

        IFinalizedResult FinalizeResult(Result result);
    }
}