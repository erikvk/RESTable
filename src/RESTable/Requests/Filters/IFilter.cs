using System.Collections.Generic;

namespace RESTable.Requests.Filters
{
    internal interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities) where T : class;
    }
}