using System.Collections.Generic;
using System.Threading;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Select operation used in GET from a view. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate IAsyncEnumerable<T> AsyncViewSelector<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;
}