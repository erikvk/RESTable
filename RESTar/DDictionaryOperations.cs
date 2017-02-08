using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Starcounter;

namespace RESTar
{
    internal class DDictionaryOperations : IOperationsProvider<DDictionary>
    {
        public IEnumerable<DDictionary> Select(IRequest request)
        {
            IEnumerable<DDictionary> all = Db.SQL<DDictionary>($"SELECT t FROM {request.Resource.FullName} t");
            if (request.OrderBy != null)
            {
                if (request.OrderBy.Ascending)
                    all = all.OrderBy(dict => dict.SafeGet(request.OrderBy.Key)?.ToString() ?? "");
                else all = all.OrderByDescending(dict => dict.SafeGet(request.OrderBy.Key)?.ToString() ?? "");
            }
            if (request.Conditions == null)
            {
                if (request.Limit < 1) return all;
                return all.Take(request.Limit);
            }
            var predicate = request.Conditions?.DDictPredicate();
            var matches = all.Where(dict => predicate(dict));
            if (request.Limit < 1) return matches;
            return matches.Take(request.Limit);
        }

        public int Insert(IEnumerable<DDictionary> entities, IRequest request)
        {
            return entities.Count();
        }

        public int Update(IEnumerable<DDictionary> entities, IRequest request)
        {
            return entities.Count();
        }

        public int Delete(IEnumerable<DDictionary> entities, IRequest request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (entity != null)
                {
                    Db.Transact(() => { entity.Delete(); });
                    count += 1;
                }
            }
            return count;
        }
    }
}