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
        public static long? RowCount(string tableName)
        {
            return Db.SQL($"SELECT COUNT(t) FROM {tableName} t").First as long?;
        }

        public static T First<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First;
        }

        public static ICollection<T> All<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").ToList();
        }

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        public static bool Exists<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whereKey.Fnuttify()} =? ", whereValue)
                       .First != null;
        }

        public static T Get<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE t.{whereKey.Fnuttify()} =? ", whereValue)
                .First;
        }
    }
}