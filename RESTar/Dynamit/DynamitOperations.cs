using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Starcounter;

namespace RESTar.Dynamit
{
    internal class DynamitOperations : IOperationsProvider<DDictionary>
    {
        public IEnumerable<DDictionary> Select(IRequest request)
        {
            IEnumerable<DDictionary> all = Db.SQL<DDictionary>($"SELECT t FROM {request.Resource.FullName} t");
            if (request.OrderBy != null)
            {
                if (request.OrderBy.Ascending)
                    all = all.OrderBy(dict => dict[request.OrderBy.Key].ToString());
                else all = all.OrderByDescending(dict => dict[request.OrderBy.Key].ToString());
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

        public void Insert(IEnumerable<DDictionary> entities, IRequest request)
        {
        }

        public void Update(IEnumerable<DDictionary> entities, IRequest request)
        {
        }

        public void Delete(IEnumerable<DDictionary> entities, IRequest request)
        {
            foreach (var entity in entities)
                Db.Transact(() => entity.Delete());
        }
    }
}