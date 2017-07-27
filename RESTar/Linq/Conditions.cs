using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using RESTar.Deflection.Dynamic;

namespace RESTar.Linq
{
    /// <summary>
    /// Extension methods for handling conditions
    /// </summary>
    public static class Conditions
    {
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
            post = conds.Where(c => !c.ScQueryable || c.IsOfType<string>()).ToList();
            return post.Any();
        }

        internal static bool HasEquality<T>(this IEnumerable<Condition<T>> conds,
            out IEnumerable<Condition<T>> equality) where T : class
        {
            equality = conds.Where(c => c.Operator.Equality).ToList();
            return equality.Any();
        }

        internal static bool HasCompare<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> compare)
            where T : class
        {
            compare = conds.Where(c => c.Operator.Compare).ToList();
            return compare.Any();
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
        public static Condition<T> Get<T>(this IEnumerable<Condition<T>> conds, string key, Operator op) where T : class
        {
            return conds.FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<Condition<T>> Redirect<T>(this IEnumerable<ICondition> conds, string direct,
            string to) where T : class
        {
            var props = typeof(T).GetStaticProperties();
            return conds.Where(cond => cond.Term.IsDynamic || props.ContainsKey(cond.Term.First?.Name.ToLower()))
                .Select(cond => direct.EqualsNoCase(cond.Key)
                    ? cond.Redirect<T>(to)
                    : cond.Redirect<T>());
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<Condition<T>> Redirect<T>(this IEnumerable<ICondition> conds,
            params (string direct, string to)[] newKeyAssignments) where T : class
        {
            var props = typeof(T).GetStaticProperties();
            return conds.Where(cond => cond.Term.IsDynamic || props.ContainsKey(cond.Term.First?.Name.ToLower()))
                .Select(cond =>
                {
                    foreach (var keyAssignment in newKeyAssignments)
                        if (keyAssignment.direct.EqualsNoCase(cond.Key))
                            return cond.Redirect<T>(keyAssignment.to);
                    return cond.Redirect<T>();
                });
        }


        internal static (bool HasChanged, bool ValueChanged) GetStatus<T>(this IEnumerable<Condition<T>> conds)
            where T : class
        {
            var ValueChanged = false;
            foreach (var cond in conds)
            {
                if (cond.HasChanged) return (true, false);
                if (cond.ValueChanged) ValueChanged = true;
            }
            return (false, ValueChanged);
        }

        internal static void ResetStatus<T>(this IEnumerable<Condition<T>> conds) where T : class
        {
            conds.ForEach(c => c.HasChanged = c.ValueChanged = false);
        }
    }
}