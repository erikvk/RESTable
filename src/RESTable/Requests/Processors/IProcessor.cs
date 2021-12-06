using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Requests.Processors;

public interface IProcessor
{
    IAsyncEnumerable<ProcessedEntity> Apply<T>(IAsyncEnumerable<T> entities, ISerializationMetadata metadata) where T : notnull;
}