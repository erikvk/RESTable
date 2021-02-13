using System;
using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources
{
    /// <summary>
    ///  Entity resource provider for entities stored in CLR memory
    /// </summary>
    internal class InMemoryEntityResourceProvider : EntityResourceProvider<object>
    {
        protected override Type AttributeType => typeof(InMemoryAttribute);

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => InMemoryOperations<T>.Select();
        protected override int DefaultInsert<T>(IRequest<T> request) => InMemoryOperations<T>.Insert(request.GetInputEntities());
        protected override int DefaultUpdate<T>(IRequest<T> request) => InMemoryOperations<T>.Update(request.GetInputEntities());
        protected override int DefaultDelete<T>(IRequest<T> request) => InMemoryOperations<T>.Delete(request.GetInputEntities());
    }
}