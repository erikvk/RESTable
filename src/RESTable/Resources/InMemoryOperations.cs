using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace RESTable.Resources;

public static class InMemoryOperations<T> where T : class
{
    static InMemoryOperations()
    {
        Store = new ConcurrentDictionary<T, Unit>();
    }

    private static IDictionary<T, Unit> Store { get; }

    public static IEnumerable<T> Select()
    {
        return Store.Keys;
    }

    public static IEnumerable<T> Select(Func<T, bool> predicate)
    {
        return Store.Keys.Where(predicate);
    }

    public static IEnumerable<T> Insert(params T[] entities)
    {
        return Insert((IEnumerable<T>) entities);
    }

    public static IEnumerable<T> Insert(IEnumerable<T> entities)
    {
        foreach (var toAdd in entities)
        {
            if (!Store.ContainsKey(toAdd))
                Store.Add(toAdd, default);
            yield return toAdd;
        }
    }

    public static IEnumerable<T> Update(params T[] entities)
    {
        return Update((IEnumerable<T>) entities);
    }

    public static IEnumerable<T> Update(IEnumerable<T> entities)
    {
        return entities;
    }

    public static int Delete(params T[] entities)
    {
        return Delete((IEnumerable<T>) entities);
    }

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
