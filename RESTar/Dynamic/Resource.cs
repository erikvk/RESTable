using System.Collections.Generic;
using RESTar.Internal.Sc;
using RESTar.Requests;
using RESTar.Resources.Operations;
using RESTar.Linq;
using RESTar.Resources;

namespace RESTar.Dynamic
{
    [RESTar]
    internal sealed class Resource : ResourceController<DynamitResourceProvider>, ISelector<Resource>, IInserter<Resource>,
        IUpdater<Resource>, IDeleter<Resource>
    {
        public IEnumerable<Resource> Select(IRequest<Resource> request) => Select<Resource>().Where(request.Conditions);

        public int Insert(IRequest<Resource> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Insert();
                i += 1;
            }
            return i;
        }

        public int Update(IRequest<Resource> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Update();
                i += 1;
            }
            return i;
        }

        public int Delete(IRequest<Resource> request)
        {
            var i = 0;
            foreach (var resource in request.GetInputEntities())
            {
                resource.Delete();
                i += 1;
            }
            return i;
        }
    }
}