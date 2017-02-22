using System.Linq;
using Starcounter;

namespace RESTar
{
    public static class StarcounterOperations
    {
        public static OperationsProvider<T> Provider<T>() => new OperationsProvider<T>
        {
            Selector = Selector<T>(),
            Inserter = Inserter<T>(),
            Updater = Updater<T>(),
            Deleter = Deleter<T>()
        };

        public static Selector<T> Selector<T>() => new Selector<T>(DbTools.StaticSelect<T>);
        public static Inserter<T> Inserter<T>() => new Inserter<T>((entities, request) => entities.Count());
        public static Updater<T> Updater<T>() => new Updater<T>((entities, request) => entities.Count());

        public static Deleter<T> Deleter<T>() => new Deleter<T>((entities, request) =>
        {
            var count = 0;
            foreach (var entity in entities)
            {
                Db.Transact(() =>
                {
                    if (entity != null)
                    {
                        entity.Delete();
                        count += 1;
                    }
                });
            }
            return count;
        });
    }
}