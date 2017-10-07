﻿using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    internal static class StarcounterOperations<T> where T : class
    {
        internal static readonly string SELECT = $"SELECT t FROM {typeof(T).FullName} t ";
        internal static readonly string COUNT = $"SELECT COUNT(t) FROM {typeof(T).FullName} t ";

        /// <summary>
        /// Selects Starcounter database resource entites
        /// </summary>
        public static Selector<T> Select => r =>
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

        /// <summary>
        /// Profiler for static Starcounter database resources (used by RESTar internally, don't use)
        /// </summary>
        public static Profiler Profile => () => ResourceProfile.MakeStarcounter(typeof(T));

        /// <summary>
        /// Counter for static Starcounter database resources (used by RESTar internally, don't use)
        /// </summary>
        public static Counter<T> Count => r =>
        {
            switch (r)
            {
                case Request<T> @internal when @internal.Conditions.HasPost(out var _):
                    return Select(r)?.LongCount() ?? 0L;
                case Request<T> @internal:
                    return Db.SQL<long>(@internal.CountQuery, @internal.SqlValues).First;
                case var external when !external.Conditions.Any():
                    return Db.SQL<long>(COUNT).First;
                case var external when external.Conditions.HasPost(out var _):
                    return Select(r)?.LongCount() ?? 0L;
                case var external:
                    var where = external.Conditions.GetSQL().MakeWhereClause();
                    return Db.SQL<long>($"{COUNT}{where.WhereString}", where.Values).First;
            }
        };
    }
}