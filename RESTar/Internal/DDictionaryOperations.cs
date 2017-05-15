using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;

namespace RESTar.Internal
{
    public static class DDictionaryOperations
    {
        private static IEnumerable<DDictionary> EqualitySQL(Condition c, string kvp)
        {
            var SQL = $"SELECT t.Dictionary FROM {kvp} t WHERE t.Key =? AND t.ValueHash {c.Operator.SQL}?";
            return Db.SQL<DDictionary>(SQL, c.Key, c.Value.GetHashCode());
        }

        private static IEnumerable<DDictionary> AllSQL(string table)
        {
            return Db.SQL<DDictionary>($"SELECT t FROM {table} t");
        }

        public static Selector<DDictionary> Select => r =>
        {
            var equalityConditions = r.Conditions?.Equality;
            if (equalityConditions?.Any() != true)
                return AllSQL(r.Resource.TargetType.FullName).Filter(r.Conditions);
            var kvpTable = r.Resource.TargetType.GetAttribute<DDictionaryAttribute>().KeyValuePairTable.FullName;
            var results = new HashSet<DDictionary>();
            equalityConditions.ForEach((cond, index) =>
            {
                if (index == 0) results.UnionWith(EqualitySQL(cond, kvpTable));
                else results.IntersectWith(EqualitySQL(cond, kvpTable));
            });
            return results.Filter(r.Conditions.Compare);
        };

        public static Inserter<DDictionary> Insert => StarcounterOperations<DDictionary>.Insert;
        public static Updater<DDictionary> Update => StarcounterOperations<DDictionary>.Update;
        public static Deleter<DDictionary> Delete => StarcounterOperations<DDictionary>.Delete;
    }
}