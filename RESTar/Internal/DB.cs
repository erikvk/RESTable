using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
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
}