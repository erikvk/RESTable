using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    public static class DDictionaryOperations<T> where T : DDictionary
    {
        /// <summary>
        /// Selects DDictionary entities from the Starcounter database
        /// </summary>
        public static Selector<T> Select { get; }

        /// <summary>
        /// Inserts DDictionary entities into the Starcounter database
        /// </summary>
        public static Inserter<T> Insert { get; }

        /// <summary>
        /// Updates DDictionary in the Starcounter database
        /// </summary>
        public static Updater<T> Update { get; }

        /// <summary>
        /// Deletes DDictionary entities from the Starcounter database
        /// </summary>
        public static Deleter<T> Delete { get; }

        /// <summary>
        /// Creates profiles for DDictionary tables
        /// </summary>
        public static Profiler<T> Profile { get; }

        static DDictionaryOperations()
        {
            Select = r =>
            {
                var finderConditions = new List<(string, Operator, dynamic)>();
                var otherConditions = new HashSet<Condition<T>>();
                foreach (var cond in r.Conditions)
                {
                    if (cond.InternalOperator.Equality && cond.Term.Count == 1)
                        finderConditions.Add((cond.Key, (Operator) cond.Operator, cond.Value));
                    else otherConditions.Add(cond);
                }
                var results = Finder<T>.Where(finderConditions.ToArray());
                return otherConditions.Any() ? results.Where(otherConditions) : results;
            };
            Insert = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetInputEntities().Count());
                return count;
            };
            Update = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetInputEntities().Count());
                return count;
            };
            Delete = r =>
            {
                var count = 0;
                Db.TransactAsync(() => r.GetInputEntities().ForEach(entity =>
                {
                    entity.Delete();
                    count += 1;
                }));
                return count;
            };
            Profile = r => ResourceProfile.Make(r, rows => rows.Sum(row => row.KeyValuePairs.Sum(kvp => kvp.ByteCount) + 16));
        }
    }
}