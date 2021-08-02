using System.Collections.Generic;

namespace RESTable.Requests.Processors
{
    public interface IProcessor
    {
        IAsyncEnumerable<ProcessedEntity> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull;
    }
}