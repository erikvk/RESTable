using System.Collections.Generic;

namespace RESTar.Operations
{
    public interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities);
    }

    public interface IProcessor
    {
        IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities);
    }

    public enum Operations
    {
        Conditions,
        Add,
        Rename,
        Select,
        OrderBy,
        Limit
    }
}