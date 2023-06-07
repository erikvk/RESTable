using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

public interface IEntityResourceOperationDefinition<TResource> where TResource : class
{
    bool RequiresAuthentication { get; }
    bool CanSelect { get; }
    bool CanInsert { get; }
    bool CanUpdate { get; }
    bool CanDelete { get; }
    bool CanCount { get; }

    IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    IAsyncEnumerable<TResource> InsertAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    IAsyncEnumerable<TResource> UpdateAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    ValueTask<long> DeleteAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    ValueTask<long> CountAsync(IRequest<TResource> request, CancellationToken cancellationToken);
    IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource> entities, RESTableContext context, CancellationToken cancellationToken);
}
