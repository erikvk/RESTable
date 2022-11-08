using System;
using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources;

/// <summary>
///     Entity resource provider for entities stored in CLR memory
/// </summary>
internal class InMemoryEntityResourceProvider : EntityResourceProvider<object>
{
    protected override Type AttributeType => typeof(InMemoryAttribute);

    protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request)
    {
        return InMemoryOperations<T>.Select();
    }

    protected override IEnumerable<T> DefaultInsert<T>(IRequest<T> request)
    {
        return InMemoryOperations<T>.Insert(request.GetInputEntities());
    }

    protected override IEnumerable<T> DefaultUpdate<T>(IRequest<T> request)
    {
        return InMemoryOperations<T>.Update(request.GetInputEntities());
    }

    protected override int DefaultDelete<T>(IRequest<T> request)
    {
        return InMemoryOperations<T>.Delete(request.GetInputEntities());
    }
}
