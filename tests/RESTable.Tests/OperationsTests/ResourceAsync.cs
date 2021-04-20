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
    public class ResourceAsync :
        ResourceOperationsBase,
        IAsyncSelector<ResourceAsync>,
        IAsyncInserter<ResourceAsync>,
        IAsyncUpdater<ResourceAsync>,
        IAsyncDeleter<ResourceAsync>,
        IAsyncCounter<ResourceAsync>,
        IAsyncAuthenticatable<ResourceAsync>
    {
        public async IAsyncEnumerable<ResourceAsync> SelectAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncSelectorWasCalled = true;
            yield break;
        }

        public async ValueTask<int> InsertAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncInserterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> UpdateAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncUpdaterWasCalled = true;
            return 0;
        }

        public async ValueTask<int> DeleteAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncDeleterWasCalled = true;
            return 0;
        }

        public async ValueTask<long> CountAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncCounterWasCalled = true;
            return 1;
        }

        public async ValueTask<AuthResults> AuthenticateAsync(IRequest<ResourceAsync> request)
        {
            request.GetRequiredService<OperationsTestsFlags>().AsyncAuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
        }
    }
}