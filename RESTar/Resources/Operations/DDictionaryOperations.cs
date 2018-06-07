using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using Starcounter;

namespace RESTar.Resources.Operations
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    public static class DDictionaryOperations<T> where T : DDictionary
    {
        /// <summary>
        /// Selects DDictionary entities from the Starcounter database
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            var finderConditions = new List<(string, Operator, dynamic)>();
            var otherConditions = new HashSet<Condition<T>>();
            foreach (var cond in request.Conditions)
            {
                if (cond.InternalOperator.Equality && cond.Term.Count == 1)
                    finderConditions.Add((cond.Key, (Operator) cond.Operator, cond.Value));
                else otherConditions.Add(cond);
            }
            var results = Finder<T>.Where(finderConditions.ToArray());
            return otherConditions.Any() ? results.Where(otherConditions) : results;
        }

        /// <summary>
        /// Inserts DDictionary entities into the Starcounter database
        /// </summary>
        public static int Insert(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Updates DDictionary in the Starcounter database
        /// </summary>
        public static int Update(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Deletes DDictionary entities from the Starcounter database
        /// </summary>
        public static int Delete(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => request.GetInputEntities().ForEach(entity =>
            {
                entity.Delete();
                count += 1;
            }));
            return count;
        }

        /// <summary>
        /// Creates profiles for DDictionary tables
        /// </summary>
        public static ResourceProfile Profile(IEntityResource<T> resource)
        {
            return ResourceProfile.Make(resource, rows => rows.Sum(row => row.KeyValuePairs.Sum(kvp => kvp.ByteCount) + 16));
        }
    }
}