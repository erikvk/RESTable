using System.Collections.Generic;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Protocols
{
    internal interface IProtocolProvider
    {
        Arguments MakeRequestArguments(string uri, byte[] body = null, IDictionary<string, string> headers = null,
            MimeType contentType = null, MimeType[] accept = null, Origin origin = null);

        IFinalizedResult FinalizeResult(Result result);

        string MakeRelativeUri(IUriParameters parameters);
    }
}