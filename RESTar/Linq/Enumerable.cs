using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTar.Linq
{
    /// <summary>
    /// Extension methods for IEnumerables
    /// </summary>
    public static class Enumerable
    {
        /// <summary>
        /// Filters an IEnumerable of resource entities and returns all entities x such that all the 
        /// conditions are true of x.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> entities, IEnumerable<Condition<T>> conditions)
            where T : class => conditions == null ? entities : conditions.Apply(entities);

        /// <summary>
        /// Generates an IEnumerable of string using the selector function applied to the source IEnumerable, 
        /// and then joins those strings using the separator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static string StringJoin<T>(this IEnumerable<T> source, string separator,
            Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            return string.Join(separator, selector(source));
        }

        ///// <summary>
        ///// Returns true if and only if the source IEnumerable contains more than one element.
        ///// Equivalent to (for some IEnumerable source) source?.Skip(1).Any() == true;
        ///// Will NOT iterate over all entitites in the source, like Count() would.
        ///// </summary>
        //public static bool MoreThanOne<T>(this IEnumerable<T> source) => source?.Skip(1).Any() == true;

        ///// <summary>
        ///// Returns true if and only if the source IEnumerable contains exacly one element.
        ///// Equivalent to (for some IEnumerable source) source?.Any() == true AND !source.Skip(1).Any();
        ///// Will NOT iterate over all entitites in the source, like Count() would.
        ///// </summary>
        //public static bool ExaclyOne<T>(this IEnumerable<T> source) => source?.Any() == true && !source.Skip(1).Any();

        /// <summary>
        /// Returns true if and only if the source IEnumerable contains two or more equal objects.
        /// If a duplicate is found, it is assigned to the out 'duplicate' variable.
        /// </summary>
        public static bool ContainsDuplicates<T>(this IEnumerable<T> source, out T duplicate)
        {
            duplicate = default(T);
            var d = new HashSet<T>();
            foreach (var t in source)
            {
                if (!d.Add(t))
                {
                    duplicate = t;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Applies an action to each element in the source IEnumerable. Equivalent to how Select works for 
        /// functions, but for actions.
        /// </summary>
        public static IEnumerable<T> Apply<T>(this IEnumerable<T> source, Action<T> action) => source.Select(e =>
        {
            action(e);
            return e;
        });

        /// <summary>
        /// Collects the elements in the source IEnumerable and applies the given function to the
        /// entire collection.
        /// </summary>
        public static T2 Collect<T1, T2>(this IEnumerable<T1> source, Func<IEnumerable<T1>, T2> action)
        {
            return action(source);
        }

        /// <summary>
        /// Collects the source dictionary and runs it through a function
        /// </summary>
        public static TResult CollectDict<TKey, TValue, TResult>(this IDictionary<TKey, TValue> source,
            Func<IDictionary<TKey, TValue>, TResult> action)
        {
            return action(source);
        }

        /// <summary>
        /// Evaluates the predicate. If the boolean condition is true, returns the result of the 'then'
        /// function applied to the source IEnumerable. Else, returns the source IEnumerable.
        /// </summary>
        public static IEnumerable<T> If<T>(this IEnumerable<T> source, bool @if,
            Func<IEnumerable<T>, IEnumerable<T>> then)
        {
            return @if ? then(source) : source;
        }

        /// <summary>
        /// Evaluates the predicate. If the boolean condition is true, returns the result of the 'then'
        /// function applied to the source IEnumerable. Else, returns the result of the 'else' function 
        /// applied to the source IEnumerable.
        /// </summary>
        public static IEnumerable<T2> If<T1, T2>(this IEnumerable<T1> source, bool @if,
            Func<IEnumerable<T1>, IEnumerable<T2>> then, Func<IEnumerable<T1>, IEnumerable<T2>> @else)
        {
            return @if ? then(source) : @else(source);
        }

        /// <summary>
        /// Evaluates the predicate. If the predicate evaluates to true, returns the result of the 'then'
        /// function applied to the source IEnumerable. Else, returns the source IEnumerable.
        /// </summary>
        public static IEnumerable<T> If<T>(this IEnumerable<T> source, Func<bool> @if,
            Func<IEnumerable<T>, IEnumerable<T>> then)
        {
            return @if() ? then(source) : source;
        }

        /// <summary>
        /// Evaluates the predicate. If the predicate evaluates to true, returns the result of the 'then'
        /// function applied to the source IEnumerable. Else, returns the source IEnumerable.
        /// </summary>
        public static IEnumerable<T> If<T>(this IEnumerable<T> source, Predicate<IEnumerable<T>> @if,
            Func<IEnumerable<T>, IEnumerable<T>> then)
        {
            return @if(source) ? then(source) : source;
        }

        /// <summary>
        /// Performs an action for each element in an IEnumerable.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var e in source) action(e);
        }

        /// <summary>
        /// Performs an action for each element in an IEnumerable. Exposes the element index.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in source) action(e, i += 1);
        }
    }
}