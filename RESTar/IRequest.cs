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
        Condition GetCondition(string key);
        string Json { get; }
    }
}