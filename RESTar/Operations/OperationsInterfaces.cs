using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar.Operations
{
    internal interface IFilter
    {
        IEnumerable<T> Apply<T>(IEnumerable<T> entities);
    }

    internal interface IProcessor
    {
        IEnumerable<JObject> Apply<T>(IEnumerable<T> entities);
    }
}