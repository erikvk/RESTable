using System.Linq;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    internal static class StarcounterOperations<T> where T : class
    {
        internal static readonly string SELECT = $"SELECT t FROM {typeof(T).FullName} t ";

        /// <summary>
        /// Selects Starcounter database resource entites
        /// </summary>
        public static Selector<T> Select => request =>
        {
            switch (request)
            {
                case Request<T> @internal:
                    var r1 = Db.SQL<T>(@internal.SqlQuery, @internal.SqlValues);
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

        /// <summary>
        /// Inserter for static Starcounter database resources (used by RESTar internally, don't use)
        /// </summary>
        public static Inserter<T> Insert => (e, r) => e.Count();

        /// <summary>
        /// Updater for static Starcounter database resources (used by RESTar internally, don't use)
        /// </summary>
        public static Updater<T> Update => (e, r) => e.Count();

        /// <summary>
        /// Deleter for static Starcounter database resources (used by RESTar internally, don't use)
        /// </summary>
        public static Deleter<T> Delete => (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
    }
}