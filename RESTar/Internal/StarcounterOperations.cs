using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
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
            IEnumerable<T> results;
            switch (request)
            {
                case ViewRequest<T> _:
                case RESTRequest<T> _:
                    if (!request.Conditions.Any())
                        return Db.SQL<T>($"{SELECT}{request.MetaConditions.OrderBy?.SQL}");
                    var where = request.Conditions.GetSQL().MakeWhereClause();
                    results = Db.SQL<T>($"{SELECT}{where.WhereString} " +
                                        $"{request.MetaConditions.OrderBy?.SQL}", where.Values);
                    return !request.Conditions.HasPost(out var post) ? results : results.Where(post);
                case Request<T> appRequest:
                    results = Db.SQL<T>(appRequest.SqlQuery, appRequest.SqlValues);
                    return !appRequest.Conditions.HasPost(out var _post) ? results : results.Where(_post);
                default: return null;
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