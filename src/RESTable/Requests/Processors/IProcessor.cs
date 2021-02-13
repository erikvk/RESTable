using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTable.Requests.Processors
{
    internal interface IProcessor
    {
        IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities);
    }
}