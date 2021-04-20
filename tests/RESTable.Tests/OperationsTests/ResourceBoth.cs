using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

#pragma warning disable 1998

namespace RESTable.Tests.OperationsTests
{
    [RESTable]
    public class ResourceBoth :
        ResourceOperationsBase,
        IAsyncSelector<ResourceBoth>,
        IAsyncInserter<ResourceBoth>,
        IAsyncUpdater<ResourceBoth>,
        IAsyncDeleter<ResourceBoth>,
        IAsyncCounter<ResourceBoth>,
        IAsyncAuthenticatable<ResourceBoth>,
        ISelector<ResourceBoth>,
        IInserter<ResourceBoth>,
        IUpdater<ResourceBoth>,
        IDeleter<ResourceBoth>,
        ICounter<ResourceBoth>,
        IAuthenticatable<ResourceBoth>
    {
        public async IAsyncEnumerable<ResourceBoth> SelectAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncSelectorWasCalled = true;
            yield break;
        }

        public async ValueTask<int> InsertAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncInserterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> UpdateAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncUpdaterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> DeleteAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncDeleterWasCalled = true;
            return 0;
        }

        public async ValueTask<long> CountAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncCounterWasCalled = true;
            return 1;
        }

        public async ValueTask<AuthResults> AuthenticateAsync(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncAuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
        }

        public IEnumerable<ResourceBoth> Select(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().SelectorWasCalled = true;
            yield break;
        }

        public int Insert(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().InserterWasCalled = true;
            return 0;
        }

        public int Update(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().UpdaterWasCalled = true;
            return 0;
        }

        public int Delete(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().DeleterWasCalled = true;
            return 0;
        }

        public long Count(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().CounterWasCalled = true;
            return 1;
        }

        public AuthResults Authenticate(IRequest<ResourceBoth> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
        }
    }
}