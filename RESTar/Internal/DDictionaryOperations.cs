using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    internal static class DDictionaryOperations<T> where T : class
    {
        private static IEnumerable<T> EqualitySQL(Condition<T> c, string kvp)
        {
            var SQL = $"SELECT CAST(t.Dictionary AS {typeof(T).FullName}) " +
                      $"FROM {kvp} t WHERE t.Key =? AND t.ValueHash {c.Operator.SQL}?";
            return Db.SQL<T>(SQL, c.Key, c.Value.GetHashCode());
        }

        private static IEnumerable<T> AllSQL => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t");

        /// <summary>
        /// Selects DDictionary entites
        /// </summary>
        public static Selector<T> Select => r =>
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

        /// <summary>
        /// Inserter for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Inserter<T> Insert => (e, r) => e.Count();

        /// <summary>
        /// Updater for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Updater<T> Update => (e, r) => e.Count();

        /// <summary>
        /// Deleter for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Deleter<T> Delete => (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
    }
}