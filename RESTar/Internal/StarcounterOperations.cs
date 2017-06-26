using System.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    public static class StarcounterOperations<T> where T : class
    {
        public static Selector<T> Select => request =>
        {
            var where = request.Conditions?.SQL?.ToWhereClause();
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t {where?.stringPart} " +
                             $"{request.MetaConditions.OrderBy?.SQL}", where?.valuesPart)
                .Filter(request.Conditions?.PostSQL);
        };

        public static Inserter<T> Insert => (e, r) => e.Count();
        public static Updater<T> Update => (e, r) => e.Count();
        public static Deleter<T> Delete => (e, r) => Do.Run(() => e.ForEach(Db.Delete), e.Count());
    }
}