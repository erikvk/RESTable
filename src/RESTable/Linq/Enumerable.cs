using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Results;

namespace RESTable.Linq
{
    /// <summary>
    /// Extension methods for IEnumerables
    /// </summary>
    public static class Enumerable
    {
        public static IAsyncEnumerable<T> ToAsyncSingleton<T>(this T item) => AsyncEnumerable.Repeat(item, 1);

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

        /// <summary>
        /// Joins strings using a separator.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string StringJoin(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        /// <summary>
        /// Returns true if and only if the source IEnumerable contains two or more equal objects.
        /// If a duplicate is found, it is assigned to the out 'duplicate' variable.
        /// </summary>
        public static bool ContainsDuplicates<T>(this IEnumerable<T> source, out T? duplicate)
        {
            duplicate = default;
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
        /// Returns true if and only if the source IEnumerable contains two or more equal objects by
        /// comparing the images of a selector function. If a duplicate is found, it is assigned to 
        /// the out 'duplicate' variable.
        /// </summary>
        public static bool ContainsDuplicates<T1, T2>(this IEnumerable<T1> source, Func<T1, T2> selector, out T1? duplicate)
        {
            duplicate = default;
            var d = new HashSet<T2>();
            foreach (var t1 in source)
            {
                var t2 = selector(t1);
                if (!d.Add(t2))
                {
                    duplicate = t1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if and only if the source IEnumerable contains two or more equal objects.
        /// If a duplicate is found, it is assigned to the out 'duplicate' variable.
        /// </summary>
        public static bool ContainsDuplicates<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer, out T? duplicate)
        {
            duplicate = default;
            var d = new HashSet<T>(comparer);
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
        /// Returns true if and only if the source IEnumerable contains two or more equal objects by
        /// comparing the images of a selector function. If a duplicate is found, it is assigned to 
        /// the out 'duplicate' variable.
        /// </summary>
        public static bool ContainsDuplicates<T1, T2>(this IEnumerable<T1> source, Func<T1, T2> selector, IEqualityComparer<T2> comparer,
            out T1? duplicate)
        {
            duplicate = default;
            var d = new HashSet<T2>(comparer);
            foreach (var t1 in source)
            {
                var t2 = selector(t1);
                if (!d.Add(t2))
                {
                    duplicate = t1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Counts the occurances of the provided character in the given string
        /// </summary>
        public static int Count(this string str, char c) => str.Count(x => x == c);

        /// <summary>
        /// Applies an action to each element in the source IEnumerable. Equivalent to how Select works for 
        /// functions, but for actions.
        /// </summary>
        public static IEnumerable<T> Apply<T>(this IEnumerable<T> source, Action<T> action) where T : class
        {
            foreach (var e in source)
            {
                action(e);
                yield return e;
            }
        }

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
        /// Splits an input IEnumerable into two lists, one for which the given predicate is true, and one
        /// for which the given predicate is false
        /// </summary>
        public static (List<T> trues, List<T> falses) Split<T>(this IEnumerable<T> source, Predicate<T> splitCondition)
        {
            var trues = new List<T>();
            var falses = new List<T>();
            foreach (var item in source)
            {
                if (splitCondition(item))
                    trues.Add(item);
                else falses.Add(item);
            }
            return (trues, falses);
        }

        internal static IAsyncEnumerable<T> UnsafeLimit<T>(this IAsyncEnumerable<T> source, bool limit = true)
        {
            if (!limit) return source;

            async IAsyncEnumerable<T> apply()
            {
                var i = 1;
                await foreach (var entity in source.ConfigureAwait(false))
                {
                    if (i > 1) throw new AmbiguousMatch();
                    i += 1;
                    yield return entity;
                }
            }

            return apply();
        }

        internal static IAsyncEnumerable<T> InputLimit<T>(this IAsyncEnumerable<T> source, bool limit = true)
        {
            if (!limit) return source;

            async IAsyncEnumerable<T> apply()
            {
                var i = 1;
                await foreach (var entity in source.ConfigureAwait(false))
                {
                    if (i > 1) throw new InvalidInputCount();
                    i += 1;
                    yield return entity;
                }
            }

            return apply();
        }
    }
}