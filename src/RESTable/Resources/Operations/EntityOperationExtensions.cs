using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Resources.Operations
{
    internal static class EntityOperationExtensions
    {
        internal static IAsyncEnumerable<T> Validate<T>(this IAsyncEnumerable<T> entities, IEntityResource<T> resource) where T : class
        {
            return resource.Validate(entities);
        }
    }
}