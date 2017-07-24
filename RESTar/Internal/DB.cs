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
    }
}