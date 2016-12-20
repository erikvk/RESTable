using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
