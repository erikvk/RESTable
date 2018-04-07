using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar.Processors
{
    internal interface IProcessor
    {
        IEnumerable<JObject> Apply<T>(IEnumerable<T> entities);
    }
}