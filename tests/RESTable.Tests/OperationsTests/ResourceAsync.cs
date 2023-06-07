using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

#pragma warning disable 1998

namespace RESTable.Tests.OperationsTests;

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
    public async ValueTask<AuthResults> AuthenticateAsync(IRequest<ResourceAsync> request, CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncAuthenticatorWasCalled = true;
        return request.Headers["FailMe"] != "yes";
    }

    public async ValueTask<long> CountAsync(IRequest<ResourceAsync> request, CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncCounterWasCalled = true;
        return 1;
    }

    public async ValueTask<long> DeleteAsync(IRequest<ResourceAsync> request, CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncDeleterWasCalled = true;
        return 0;
    }

    public async IAsyncEnumerable<ResourceAsync> InsertAsync(IRequest<ResourceAsync> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncInserterWasCalled = true;
        yield break;
    }

    public async IAsyncEnumerable<ResourceAsync> SelectAsync(IRequest<ResourceAsync> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncSelectorWasCalled = true;
        yield break;
    }

    public async IAsyncEnumerable<ResourceAsync> UpdateAsync(IRequest<ResourceAsync> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        request.GetRequiredService<OperationsTestsFlags>().AsyncUpdaterWasCalled = true;
        yield break;
    }
}
