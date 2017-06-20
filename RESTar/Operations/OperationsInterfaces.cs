using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar.Operations
{
    public interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities);
    }

    public interface IProcessor
    {
        IEnumerable<JObject> Apply<T>(IEnumerable<T> entities);
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