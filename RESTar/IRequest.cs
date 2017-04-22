using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar
{
    public interface IRequest
    {
        Conditions Conditions { get; }
        RESTarMethods Method { get; }
        IResource Resource { get; }
        OrderBy OrderBy { get; }
        int Limit { get; }
        bool Unsafe { get; }
        string Body { get; }
        string AuthToken { get; }
        IDictionary<string, string> ResponseHeaders { get; }
    }
}