using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

#pragma warning disable 1998

namespace RESTable.Tests
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
            request.GetService<OperationsTestsFlags>().AsyncSelectorWasCalled = true;
            yield break;
        }

        public async ValueTask<int> InsertAsync(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AsyncInserterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> UpdateAsync(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AsyncUpdaterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> DeleteAsync(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AsyncDeleterWasCalled = true;
            return 0;
        }

        public async ValueTask<long> CountAsync(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AsyncCounterWasCalled = true;
            return 1;
        }

        public async ValueTask<AuthResults> AuthenticateAsync(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AsyncAuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
        }

        public IEnumerable<ResourceBoth> Select(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().SelectorWasCalled = true;
            yield break;
        }

        public int Insert(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().InserterWasCalled = true;
            return 0;
        }

        public int Update(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().UpdaterWasCalled = true;
            return 0;
        }

        public int Delete(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().DeleterWasCalled = true;
            return 0;
        }

        public long Count(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().CounterWasCalled = true;
            return 1;
        }

        public AuthResults Authenticate(IRequest<ResourceBoth> request)
        {
            request.GetService<OperationsTestsFlags>().AuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
        }
    }
}