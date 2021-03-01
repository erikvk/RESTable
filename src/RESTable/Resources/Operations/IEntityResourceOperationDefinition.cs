using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    public interface IEntityResourceOperationDefinition<TResource> where TResource : class
    {
        bool RequiresAuthentication { get; }
        bool CanSelect { get; }
        bool CanInsert { get; }
        bool CanUpdate { get; }
        bool CanDelete { get; }
        bool CanCount { get; }

        IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request);
        ValueTask<int> InsertAsync(IRequest<TResource> request);
        ValueTask<int> UpdateAsync(IRequest<TResource> request);
        ValueTask<int> DeleteAsync(IRequest<TResource> request);
        ValueTask<AuthResults> AuthenticateAsync(IRequest<TResource> request);
        ValueTask<long> CountAsync(IRequest<TResource> request);
        IAsyncEnumerable<TResource> Validate(IAsyncEnumerable<TResource> entities, RESTableContext context);
    }
}