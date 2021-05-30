using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Linq
{
    /// <summary>
    /// Extension methods for handling conditions
    /// </summary>
    public static class Conditions
    {
        /// <summary>
        /// Filters an IEnumerable of resource entities and returns all entities x such that all the 
        /// conditions are true of x.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> entities, IEnumerable<Condition<T>> conditions)
            where T : class
        {
            return entities.Where(entity => conditions.All(condition => condition.HoldsFor(entity)));
        }

        /// <summary>
        /// Returns true if and only if all the given conditions hold for the given subject
        /// </summary>
        public static bool AllHoldFor<T>(this IEnumerable<Condition<T>> conditions, T subject) where T : class
        {
            return conditions.All(condition => condition.HoldsFor(subject));
        }

        /// <summary>
        /// Access all conditions with a given key (case insensitive)
        /// </summary>
        public static IEnumerable<Condition<T>> Get<T>(this IEnumerable<Condition<T>> conds, string key) where T : class
        {
            return conds.Where(c => c.Key.EqualsNoCase(key));
        }

        /// <summary>
        /// Access a condition by its key (case insensitive) and operator
        /// </summary>
        public static Condition<T>? Get<T>(this IEnumerable<Condition<T>> conds, string key, Operators op) where T : class
        {
            return conds.FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));
        }

        /// <summary>
        /// Gets the first condition from a collection by its key (case insensitive) and operator, and removes
        /// it from the collection. 
        /// </summary>
        public static Condition<T>? Pop<T>(this ICollection<Condition<T>> conds, string key, Operators op) where T : class
        {
            var match = conds.Get(key, op);
            if (match is not null)
                conds.Remove(match);
            return match;
        }
    }
}