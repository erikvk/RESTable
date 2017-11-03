using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using Starcounter.Metadata;
using static System.Reflection.BindingFlags;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    internal static class StarcounterOperations<T> where T : class
    {
        internal static readonly string SELECT = $"SELECT t FROM {typeof(T).FullName.Fnuttify()} t ";
        internal static readonly string COUNT = $"SELECT COUNT(t) FROM {typeof(T).FullName.Fnuttify()} t ";
        internal static readonly Selector<T> Select;
        internal static readonly Inserter<T> Insert;
        internal static readonly Updater<T> Update;
        internal static readonly Deleter<T> Delete;
        internal static readonly Profiler<T> Profile;
        internal static readonly Counter<T> Count;

        static StarcounterOperations()
        {
            Select = r =>
            {
                switch (r)
                {
                    case Request<T> @internal:
                        var r1 = Db.SQL<T>(@internal.SelectQuery, @internal.SqlValues);
                        return !@internal.Conditions.HasPost(out var _post) ? r1 : r1.Where(_post);
                    case var external when !external.Conditions.Any():
                        return Db.SQL<T>($"{SELECT}{external.MetaConditions.OrderBy?.SQL}");
                    case var external:
                        var where = external.Conditions.GetSQL().MakeWhereClause();
                        var r2 = Db.SQL<T>($"{SELECT}{where.WhereString} " +
                                           $"{external.MetaConditions.OrderBy?.SQL}", where.Values);
                        return !external.Conditions.HasPost(out var post) ? r2 : r2.Where(post);
                }
            };
            Insert = (e, r) => e.Count();
            Update = (e, r) => e.Count();
            Delete = (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
            Count = r =>
            {
                switch (r)
                {
                    case Request<T> @internal when @internal.Conditions.HasPost(out var _): return Select(r)?.LongCount() ?? 0L;
                    case Request<T> @internal: return Db.SQL<long>(@internal.CountQuery, @internal.SqlValues).First;
                    case var external when !external.Conditions.Any(): return Db.SQL<long>(COUNT).First;
                    case var external when external.Conditions.HasPost(out var _): return Select(r)?.LongCount() ?? 0L;
                    case var external:
                        var where = external.Conditions.GetSQL().MakeWhereClause();
                        return Db.SQL<long>($"{COUNT}{where.WhereString}", where.Values).First;
                }
            };
            Profile = r => ResourceProfile.Make(r, rows =>
            {
                const string columnSQL = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
                var resourceSQLName = typeof(T).FullName;
                var scColumns = Db.SQL<Column>(columnSQL, resourceSQLName).Select(c => c.Name).ToList();
                var columns = typeof(T).GetProperties(Instance | Public)
                    .Where(p => scColumns.Contains(p.Name))
                    .ToList();
                return rows.Sum(e => columns.Sum(p => p.ByteCount(e)) + 16);
            });
        }
    }
}