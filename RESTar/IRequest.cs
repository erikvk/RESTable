using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    public interface IRequest
    {
        Conditions Conditions { get; }
        MetaConditions MetaConditions { get; }
        IResource Resource { get; }
        RESTarMethods Method { get; }
        string Body { get; }
        string AuthToken { get; }
        IDictionary<string, string> ResponseHeaders { get; }
    }
}