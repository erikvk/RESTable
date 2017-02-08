using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar
{
    public class StarcounterOperations : IOperationsProvider<object>
    {
        public IEnumerable<dynamic> Select(IRequest request)
        {
            return DB.Select(request);
        }

        public int Insert(IEnumerable<dynamic> entities, IRequest request)
        {
            return entities.Count();
        }

        public int Update(IEnumerable<dynamic> entities, IRequest request)
        {
            return entities.Count();
        }

        public int Delete(IEnumerable<dynamic> entities, IRequest request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                Db.Transact(() =>
                {
                    if (entity != null)
                    {
                        Db.Delete(entity);
                        count += 1;
                    }
                });
            }
            return count;
        }
    }
}