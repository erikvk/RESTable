using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Linq;

/// <summary>
///     Extension methods for handling conditions
/// </summary>
public static class Conditions
{
    /// <summary>
    ///     Filters an IEnumerable of resource entities and returns all entities x such that all the
    ///     conditions are true of x.
    /// </summary>
    public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> entities, IEnumerable<Condition<T>> conditions)
        where T : class
    {
        var conditionsArray = conditions.ToArray();
        await foreach (var entity in entities)
        {
            var allHold = true;
            foreach (var condition in conditionsArray)
                if (!await condition.HoldsFor(entity).ConfigureAwait(false))
                {
                    allHold = false;
                    break;
                }
            if (allHold) yield return entity;
        }
    }

    /// <summary>
    ///     Access all conditions with a given key (case insensitive)
    /// </summary>
    public static IEnumerable<Condition<T>> Get<T>(this IEnumerable<Condition<T>> conds, string key) where T : class
    {
        return conds.Where(c => c.Key.EqualsNoCase(key));
    }

    /// <summary>
    ///     Access a condition by its key (case insensitive) and operator
    /// </summary>
    public static Condition<T>? Get<T>(this IEnumerable<Condition<T>> conds, string key, Operators op) where T : class
    {
        return conds.FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));
    }

    /// <summary>
    ///     Access a condition by its key (case insensitive) and operator
    /// </summary>
    public static bool HasParameter<TResource, TValue>(this IEnumerable<Condition<TResource>> conds, string key, out TValue? value) where TResource : class
    {
        var parameter = conds.FirstOrDefault(c => c.Operator == Operators.EQUALS && c.Key.EqualsNoCase(key));
        if (parameter is not null)
        {
            if (parameter.Value is null)
            {
                value = default;
                return true;
            }
            if (parameter.Value is TValue _value)
            {
                value = _value;
                return true;
            }
            try
            {
                value = (TValue) Convert.ChangeType(parameter.Value, typeof(TValue));
                return true;
            }
            catch
            {
                // Fall through
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    ///     Gets the first condition from a collection by its key (case insensitive) and operator, and removes
    ///     it from the collection.
    /// </summary>
    public static Condition<T>? Pop<T>(this ICollection<Condition<T>> conds, string key, Operators op) where T : class
    {
        var match = conds.Get(key, op);
        if (match is not null)
            conds.Remove(match);
        return match;
    }
}