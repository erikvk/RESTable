using System.Collections.Generic;
using RESTable.Meta;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    internal static class EntityOperationExtensions
    {
        internal static IAsyncEnumerable<T> Validate<T>(this IAsyncEnumerable<T> entities, IEntityResource<T> resource, RESTableContext context) where T : class
        {
            return resource.Validate(entities, context);
        }
    }
}