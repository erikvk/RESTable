using System.Collections.Generic;

namespace RESTar.Operations
{
    internal interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities);
    }
}