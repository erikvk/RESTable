using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RESTable.Resources
{
    public static class InMemoryOperations<T> where T : class
    {
        private static IDictionary<T, byte> Store { get; }

        static InMemoryOperations() => Store = new ConcurrentDictionary<T, byte>();

        public static IEnumerable<T> Select() => Store.Keys;

        public static int Insert(params T[] entities) => Insert((IEnumerable<T>) entities);

        public static int Insert(IEnumerable<T> entities)
        {
            var count = 0;
            foreach (var toAdd in entities)
            {
                if (!Store.ContainsKey(toAdd))
                    Store.Add(toAdd, 0);
                count += 1;
            }
            return count;
        }

        public static int Update(params T[] entities) => Update((IEnumerable<T>) entities);

        public static int Update(IEnumerable<T> entities) => entities.Count();    

        public static int Delete(params T[] entities) => Delete((IEnumerable<T>) entities);

        public static int Delete(IEnumerable<T> entities)
        {
            var count = 0;
            foreach (var toDelete in entities)
            {
                Store.Remove(toDelete);
                count += 1;
            }
            return count;
        }
    }
}