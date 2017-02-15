using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Starcounter;

namespace RESTar
{
    internal class DDictionaryOperations : IOperationsProvider<DDictionary>
    {
        private static IEnumerable<DDictionary> SelectSlow(IRequest request)
        {
            IEnumerable<DDictionary> all = Db.SQL<DDictionary>($"SELECT t FROM {request.Resource.FullName} t");
            if (request.OrderBy != null)
            {
                if (request.OrderBy.Ascending)
                    all = all.OrderBy<DDictionary, string>(
                        dict => dict.SafeGet(request.OrderBy.Key)?.ToString() ?? "",
                        new NumericComparer());
                else
                    all = all.OrderByDescending<DDictionary, string>(
                        dict => dict.SafeGet(request.OrderBy.Key)?.ToString() ?? "",
                        new NumericComparer());
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

        public IEnumerable<DDictionary> Select(IRequest request)
        {
            if (request.Conditions == null || request.OrderBy != null)
                return SelectSlow(request);
            var kvpTable = request.Resource.GetAttribute<DDictionaryAttribute>().KeyValuePairTable;
            var equalityConds = request.Conditions.Where(c => c.Operator.Common == "=").ToList();
            var comparisonConds = request.Conditions?.Except(equalityConds);
            IEnumerable<DDictionary> matches = new HashSet<DDictionary>();
            if (equalityConds.Any())
            {
                var first = true;
                foreach (var econd in equalityConds)
                {
                    if (first)
                    {
                        ((HashSet<DDictionary>) matches).UnionWith(
                            Db.SQL<DDictionary>(
                                $"SELECT t.Dictionary FROM {kvpTable} t WHERE t.Key=? AND t.ValueHash=? ",
                                econd.Key,
                                econd.Value?.GetHashCode()
                            )
                        );
                        first = false;
                    }
                    else
                    {
                        ((HashSet<DDictionary>) matches).IntersectWith(
                            Db.SQL<DDictionary>(
                                $"SELECT t.Dictionary FROM {kvpTable} t WHERE t.Key=? AND t.ValueHash=? ",
                                econd.Key,
                                econd.Value?.GetHashCode()
                            )
                        );
                    }
                }
            }
            else
            {
                matches = Db.SQL<DDictionary>($"SELECT t FROM {request.Resource.FullName} t");
            }

            if (comparisonConds.Any())
            {
                var predicate = comparisonConds.DDictPredicate();
                matches = matches?.Where(predicate.Invoke);
            }
            if (request.Limit < 1) return matches;
            return matches?.Take(request.Limit);
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