using System;
using System.Collections.Generic;

namespace RESTar
{
    public interface IRequest
    {
        string ResourceArgument { get; }
        IList<Condition> Conditions { get; }
        IDictionary<string, object> MetaConditions { get; }
        RESTarMethods Method { get; }
        Type Resource { get; }
        OrderBy OrderBy { get; }
        int Limit { get; }
        bool Unsafe { get; }
    }
}