using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// This class provides static methods for database queries.
    /// </summary>
    internal static class DB
    {
        public static long? RowCount(string tableName) => Db.SQL($"SELECT COUNT(t) FROM {tableName} t").First as long?;
        public static T First<T>() where T : class => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First;
        public static ICollection<T> All<T>() => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").ToList();
        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        public static bool Exists<T>(string wk, object wv) =>
            !string.IsNullOrEmpty(wk) && Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE t.{wk.Fnuttify()} =? ",
                wv).First != null;

        public static T Get<T>(string whereKey, object whereValue) => !string.IsNullOrEmpty(whereKey)
            ? Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE t.{whereKey.Fnuttify()} =? ", whereValue).First
            : default(T);
    }

    internal static class Transactions
    {
        #region Simple

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope and
        /// returns the result of the transaction. Uses Db.TransactAsync
        /// </summary>
        public static T Trans<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var result = default(T);
            Db.TransactAsync(() => result = action());
            return result;
        }

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope. 
        /// Uses Db.TransactAsync
        /// </summary>
        public static void Trans(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Db.TransactAsync(action);
        }

        /// <summary>
        /// Performs the action delegate synchronously inside a transaction scope. 
        /// Uses Db.TransactAsync
        /// </summary>
        public static void TransAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            Scheduling.ScheduleTask(() => Db.TransactAsync(action));
        }

        #endregion
    }
}