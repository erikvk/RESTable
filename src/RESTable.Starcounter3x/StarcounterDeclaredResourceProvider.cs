using System;
using System.Collections.Generic;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using Starcounter.Database;

namespace RESTable.Starcounter3x
{
    internal class StarcounterDeclaredResourceProvider : EntityResourceProvider<object>
    {
        protected override Type AttributeType { get; } = null;
        protected override IDatabaseIndexer DatabaseIndexer { get; } = null;
        
        protected override void Validate() { }

        protected override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
            return type.HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
        }

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => StarcounterOperations<T>.Select(request);
        protected override int DefaultInsert<T>(IRequest<T> request) => StarcounterOperations<T>.Insert(request);
        protected override int DefaultUpdate<T>(IRequest<T> request) => StarcounterOperations<T>.Update(request);
        protected override int DefaultDelete<T>(IRequest<T> request) => StarcounterOperations<T>.Delete(request);

        protected override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);
    }
}