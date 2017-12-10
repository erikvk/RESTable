using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    internal static class DDictionaryOperations<T> where T : DDictionary
    {
        private static IEnumerable<T> EqualitySQL(Condition<T> c, string kvp) => Db.SQL<T>(
            $"SELECT CAST(t.Dictionary AS {typeof(T).FullName}) FROM {kvp} t WHERE t.Key =? " +
            $"AND t.ValueHash {c.InternalOperator.SQL}?", c.Key, c.Value.GetHashCode()
        );

        private static IEnumerable<T> AllSQL => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t");
        internal static readonly Selector<T> Select;
        internal static readonly Inserter<T> Insert;
        internal static readonly Updater<T> Update;
        internal static readonly Deleter<T> Delete;
        internal static readonly Profiler<T> Profile;
        internal static readonly Counter<T> Count;

        private static (string, Dynamit.Operator, dynamic)? ToFinderCond(Condition<T> c)
        {
            return (c.Key, (Dynamit.Operator) c.Operator, c.Value);
        }

        static DDictionaryOperations()
        {
            Select = r =>
            {
                var finderConditions = new List<(string, Dynamit.Operator, dynamic)?>();
                var otherConditions = new HashSet<Condition<T>>();
                foreach (var cond in r.Conditions)
                {
                    if (cond.InternalOperator.Equality && cond.Term.Count == 1 && cond.Term.IsDynamic)
                        finderConditions.Add(ToFinderCond(cond));
                    else otherConditions.Add(cond);
                }
                var results = Finder<T>.Where(finderConditions.ToArray());
                return otherConditions.Any() ? results.Where(otherConditions) : results;
            };
            Insert = (e, r) => e.Count();
            Update = (e, r) => e.Count();
            Delete = (e, r) =>
            {
                var count = 0;
                foreach (var _e in e)
                {
                    _e.Delete();
                    count += 1;
                }
                return count;
            };
            Count = r =>
            {
                if (r.MetaConditions.Distinct != null)
                    return r.MetaConditions.Distinct.Apply(Select(r))?.LongCount() ?? 0L;
                switch (r.Conditions.Length)
                {
                    case 0: return Db.SQL<long>($"SELECT COUNT(t) FROM {typeof(T).FullName} t").FirstOrDefault();
                    default: return Select(r)?.LongCount() ?? 0;
                }
            };
            Profile = r => ResourceProfile.Make(r, rows => rows.Sum(row => row.KeyValuePairs.Sum(kvp => kvp.ByteCount) + 16));
        }
    }
}