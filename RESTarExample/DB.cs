using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTarExample
{
    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
    /// </summary>
    internal static class DB
    {
        private static readonly MethodInfo SQL = typeof(Db).GetMethods()
            .First(m => m.Name == "SQL" && m.IsGenericMethod);

        #region Get methods

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

        public static ICollection<T> All<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE {whereKey} =? ", whereValue).ToList();
        }

        public static bool Exists<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First != null;
        }

        public static bool Exists<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE {whereKey} =? ", whereValue).First != null;
        }

        public static bool Exists<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1} =? AND t.{whKey2} =? ", whValue1, whValue2).First != null;
        }

        public static T Get<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whereKey} =? ", whereValue).First;
        }

        public static T Get<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1} =? AND t.{whKey2} =? ", whValue1, whValue2).First;
        }

        public static T Get<T>(Dictionary<string, object> whereKeyValuePairs) where T : class
        {
            if (whereKeyValuePairs == null || !whereKeyValuePairs.Any()) return null;
            var whereKeys = string.Join(" AND ", whereKeyValuePairs.Select(pair => $"t.{pair.Key} =?"));
            var whereValues = whereKeyValuePairs.Values.ToArray();
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE {whereKeys}", whereValues).First();
        }

        public static IEnumerable<TValue> Domain<TTable, TValue>(string key) where TTable : class
        {
            return Db.SQL<TValue>($"SELECT t.{key} from {typeof(TTable).FullName} t").Distinct();
        }

        #endregion
    }
}