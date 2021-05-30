using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTable.Requests.Processors
{
    public interface IProcessor
    {
        IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities);
    }
}