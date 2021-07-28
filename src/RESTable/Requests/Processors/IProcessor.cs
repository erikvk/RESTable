using System.Collections.Generic;

namespace RESTable.Requests.Processors
{
    public interface IProcessor
    {
        IAsyncEnumerable<object> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull;
    }
}