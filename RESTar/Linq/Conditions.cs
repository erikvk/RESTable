using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using RESTar.Reflection.Dynamic;

namespace RESTar.Linq
{
    /// <summary>
    /// Extension methods for handling conditions
    /// </summary>
    public static class Conditions
    {
        #region Internal

        internal static bool HasSQL<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> sql)
            where T : class
        {
            sql = conds.Where(c => c.ScQueryable).ToList();
            return sql.Any();
        }

        internal static IEnumerable<Condition<T>> GetSQL<T>(this IEnumerable<Condition<T>> conds) where T : class
        {
            return conds.Where(c => c.ScQueryable);
        }

        internal static bool HasPost<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> post)
            where T : class
        {
            post = conds.Where(c => !c.ScQueryable || c.IsOfType<string>() && c.Value != null).ToList();
            return post.Any();
        }

        #endregion

        /// <summary>
        /// Filters an IEnumerable of resource entities and returns all entities x such that all the 
        /// conditions are true of x.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> entities, IEnumerable<Condition<T>> conditions)
            where T : class
        {
            if (conditions == null) return entities;
            return entities?.Where(entity => conditions.All(condition => condition.HoldsFor(entity)));
        }

        /// <summary>
        /// Returns true if and only if all the given conditions hold for the given subject
        /// </summary>
        public static bool AllHoldFor<T>(this IEnumerable<Condition<T>> conditions, T subject) where T : class
        {
            if (conditions == null) return true;
            return conditions.All(condition => condition.HoldsFor(subject));
        }

        /// <summary>
        /// Adds a new condition to the condition collection
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        /// <param name="conds">The condition collection</param>
        /// <param name="key">The key of the new condition</param>
        /// <param name="op">The operator of the new condition</param>
        /// <param name="value">The value of the new condition</param>
        public static void Add<T>(this List<Condition<T>> conds, string key, Operators op, object value) where T : class
        {
            conds.Add(new Condition<T>(key, op, value));
        }

        /// <summary>
        /// Adds new conditions to the condition collection
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public static void AddRange<T>(this List<Condition<T>> conds, params (string key, Operators op, object value)[] conditions) where T : class
        {
            foreach (var (key, op, value) in conditions)
                conds.Add(new Condition<T>(key, op, value));
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
        public static Condition<T> Get<T>(this IEnumerable<Condition<T>> conds, string key, Operators op) where T : class
        {
            return conds.FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<Condition<T>> Redirect<T>(this IEnumerable<ICondition> conds, string direct, string to) where T : class
        {
            var props = typeof(T).GetDeclaredProperties();
            return conds
                .Where(cond => cond.Term.IsDynamic || props.ContainsKey(cond.Term.First.Name))
                .Select(cond => direct.EqualsNoCase(cond.Key) ? cond.Redirect<T>(to) : cond.Redirect<T>());
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<Condition<T>> Redirect<T>(this IEnumerable<ICondition> conds, params (string direct, string to)[] newKeyAssignments)
            where T : class
        {
            var props = typeof(T).GetDeclaredProperties();
            return conds
                .Where(cond => cond.Term.IsDynamic || props.ContainsKey(cond.Term.First.Name))
                .Select(cond =>
                {
                    foreach (var (direct, to) in newKeyAssignments)
                        if (direct.EqualsNoCase(cond.Key))
                            return cond.Redirect<T>(to);
                    return cond.Redirect<T>();
                });
        }
    }
}