using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests.OperationsTests;

[RESTable]
public class ResourceSync :
    ResourceOperationsBase<ResourceSync>,
    ISelector<ResourceSync>,
    IInserter<ResourceSync>,
    IUpdater<ResourceSync>,
    IDeleter<ResourceSync>,
    IValidator<ResourceSync>,
    ICounter<ResourceSync>,
    IAuthenticatable<ResourceSync>
{
    public AuthResults Authenticate(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().AuthenticatorWasCalled = true;
        return request.Headers["FailMe"] != "yes";
    }

    public long Count(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().CounterWasCalled = true;
        return 1;
    }

    public int Delete(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().DeleterWasCalled = true;
        return 0;
    }

    public IEnumerable<ResourceSync> Insert(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().InserterWasCalled = true;
        yield break;
    }

    public IEnumerable<ResourceSync> Select(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().SelectorWasCalled = true;
        return Entities;
    }

    public IEnumerable<ResourceSync> Update(IRequest<ResourceSync> request)
    {
        request.GetRequiredService<OperationsTestsFlags>().UpdaterWasCalled = true;
        yield break;
    }

    public IEnumerable<InvalidMember> GetInvalidMembers(ResourceSync entity, RESTableContext context)
    {
        context.GetRequiredService<OperationsTestsFlags>().ValidatorWasCalled = true;
        if (entity.Id == 99)
            yield return this.MemberInvalid(p => p.Id, "Cannot be 99");
    }
}