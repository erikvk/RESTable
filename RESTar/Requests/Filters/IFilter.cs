using System.Collections.Generic;

namespace RESTar.Requests.Filters
{
    internal interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities);
    }
}