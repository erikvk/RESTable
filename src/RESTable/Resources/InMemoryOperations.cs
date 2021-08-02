using System;
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

        public static IEnumerable<T> Select(Func<T, bool> predicate) => Store.Keys.Where(predicate);

        public static IEnumerable<T> Insert(params T[] entities) => Insert((IEnumerable<T>) entities);

        public static IEnumerable<T> Insert(IEnumerable<T> entities)
        {
            foreach (var toAdd in entities)
            {
                if (!Store.ContainsKey(toAdd))
                    Store.Add(toAdd, 0);
                yield return toAdd;
            }
        }

        public static IEnumerable<T> Update(params T[] entities) => Update((IEnumerable<T>) entities);

        public static IEnumerable<T> Update(IEnumerable<T> entities) => entities;

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