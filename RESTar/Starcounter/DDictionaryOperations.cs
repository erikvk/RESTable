using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Starcounter
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    internal static class DDictionaryOperations<T> where T : DDictionary
    {
        internal static readonly Selector<T> Select;
        internal static readonly Inserter<T> Insert;
        internal static readonly Updater<T> Update;
        internal static readonly Deleter<T> Delete;
        internal static readonly Profiler<T> Profile;
        internal static readonly Counter<T> Count;

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
                Db.TransactAsync(() => count = r.GetEntities().Count());
                return count;
            };
            Update = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetEntities().Count());
                return count;
            };
            Delete = r =>
            {
                var count = 0;
                Db.TransactAsync(() => r.GetEntities().ForEach(entity =>
                {
                    entity.Delete();
                    count += 1;
                }));
                return count;
            };
            Count = null;
            Profile = r => ResourceProfile.Make(r, rows => rows.Sum(row => row.KeyValuePairs.Sum(kvp => kvp.ByteCount) + 16));
        }
    }
}