using System.Linq;
using RESTar.Operations;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.Internal
{
    internal static class ScOperations<T> where T : class
    {
        public static Selector<T> Select => r =>
        {
            var request = (Request) r;
            var where = r.Conditions?.StarcounterQueryable?.ToWhereClause();
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t {where?.stringPart} " +
                             $"{request.MetaConditions.OrderBy?.SQL}", where?.valuesPart)
                .Filter(r.Conditions?.NonStarcounterQueryable);
        };

        public static Inserter<T> Insert => (e, r) => e.Count();
        public static Updater<T> Update => (e, r) => e.Count();
        public static Deleter<T> Delete => (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
    }
}