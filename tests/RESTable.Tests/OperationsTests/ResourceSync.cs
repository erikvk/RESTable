using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Tests
{
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
        public IEnumerable<ResourceSync> Select(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().SelectorWasCalled = true;
            return Entities;
        }

        public int Insert(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().InserterWasCalled = true;
            return 0;
        }

        public int Update(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().UpdaterWasCalled = true;
            return 0;
        }

        public int Delete(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().DeleterWasCalled = true;
            return 0;
        }

        public IEnumerable<InvalidMember> Validate(ResourceSync entity, RESTableContext context)
        {
            context.Services.GetService<OperationsTestsFlags>().ValidatorWasCalled = true;
            if (entity.Id == 99)
                yield return this.Invalidate(p => p.Id, "Cannot be 99");
        }

        public long Count(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().CounterWasCalled = true;
            return 1;
        }

        public AuthResults Authenticate(IRequest<ResourceSync> request)
        {
            request.GetService<OperationsTestsFlags>().AuthenticatorWasCalled = true;
            return request.Headers["FailMe"] != "yes";
            
        }
    }
}