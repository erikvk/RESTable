using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    internal static class DDictionaryOperations<T> where T : DDictionary
    {
        private static IEnumerable<T> EqualitySQL(Condition<T> c, string kvp) => Db.SQL<T>(
            $"SELECT CAST(t.Dictionary AS {typeof(T).FullName}) FROM {kvp} t WHERE t.Key =? " +
            $"AND t.ValueHash {c.Operator.SQL}?", c.Key, c.Value.GetHashCode()
        );

        private static IEnumerable<T> AllSQL => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t");
        internal static readonly Selector<T> Select;
        internal static readonly Inserter<T> Insert;
        internal static readonly Updater<T> Update;
        internal static readonly Deleter<T> Delete;
        internal static readonly Profiler Profile;
        internal static readonly Counter<T> Count;

        static DDictionaryOperations()
        {
            Select = r =>
            {
                if (!r.Conditions.HasEquality(out var eqalityConds))
                    return AllSQL.Where(r.Conditions);
                var kvpTable = TableInfo<T>.KvpTable;
                var results = new HashSet<T>();
                eqalityConds.ForEach((cond, index) =>
                {
                    if (index == 0) results.UnionWith(EqualitySQL(cond, kvpTable));
                    else results.IntersectWith(EqualitySQL(cond, kvpTable));
                });
                return r.Conditions.HasCompare(out var compare) ? results.Where(compare) : results;
            };
            Insert = (e, r) => e.Count();
            Update = (e, r) => e.Count();
            Delete = (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
            Count = r =>
            {
                switch (r.Conditions.Length)
                {
                    case 0: return Db.SQL<long>($"SELECT COUNT(t) FROM {typeof(T).FullName} t").First;
                    default: return Select(r)?.Count() ?? 0;
                }
            };
            Profile = () => ResourceProfile.Make<T>(rows => rows.Sum(row => row.KeyValuePairs.Sum(kvp => kvp.ByteCount) + 16));
        }
    }
}