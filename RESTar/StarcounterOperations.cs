using System.Collections.Generic;
using Starcounter;

namespace RESTar
{
    public class StarcounterOperations : IOperationsProvider<object>
    {
        public IEnumerable<dynamic> Select(IRequest request)
        {
            return DB.Select(request);
        }

        public void Insert(IEnumerable<dynamic> entities, IRequest request)
        {
        }

        public void Update(IEnumerable<dynamic> entities, IRequest request)
        {
        }

        public void Delete(IEnumerable<dynamic> entities, IRequest request)
        {
            foreach (var entity in entities)
                Db.Transact(() => { Db.Delete(entity); });
        }
    }
}