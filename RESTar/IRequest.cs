using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar
{
    public interface IRequest
    {
        Conditions Conditions { get; }
        MetaConditions MetaConditions { get; }
        RESTarMethods Method { get; }
        IResource Resource { get; }
        string Body { get; }
        string AuthToken { get; }
        IDictionary<string, string> ResponseHeaders { get; }
    }
}