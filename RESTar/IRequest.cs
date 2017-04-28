using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    public interface IRequest
    {
        Conditions Conditions { get; }
        IResource Resource { get; }
        string Body { get; }
        string AuthToken { get; }
        IDictionary<string, string> ResponseHeaders { get; }
    }
}