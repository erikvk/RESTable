using System.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        /// <summary>
        /// Selects Starcounter database resource entites
        /// </summary>
        public static Selector<T> Select => request =>
        {
            if (request.Conditions == null)
                return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                                 $"{request.MetaConditions.OrderBy?.SQL}");
            var where = request.Conditions?.SQL?.MakeWhereClause();
            var results = Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t {where?.WhereString} " +
                                    $"{request.MetaConditions.OrderBy?.SQL}", where?.Values);
            return !request.Conditions.HasPost ? results : results.Filter(request.Conditions?.PostSQL);
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