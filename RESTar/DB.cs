using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar
{
    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
    /// </summary>
    internal static class DB
    {
        #region Get methods

        public static long RowCount(string tableName)
        {
            return (long) Db.SQL($"SELECT COUNT(t) FROM {tableName} t").First;
        }

        /// <summary>
        /// Fetches an object from the database.
        /// Equivalent to SQL: SELECT TOP(1) * FROM T
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <returns>The first database entity from the specified type</returns>
        public static T First<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First;
        }

        /// <summary>
        /// Fetches a list of objects from the database.
        /// Equivalent to SQL: SELECT * FROM T
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <returns>A list of all database entities from the specified type</returns>
        public static ICollection<T> All<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").ToList();
        }

        /// <summary>
        /// Fetches a list of objects from the database.
        /// Equivalent to SQL: SELECT * FROM T WHERE key = value
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <param name="whereKey">The key used in the WHERE clause, if null, then no WHERE clause is added</param>
        /// <param name="whereValue">The value used in the WHERE clause, if key is null then no WHERE clause is added</param>
        /// <returns>A list of database entities from the specified type that matches the value on the specified key</returns>
        public static ICollection<T> All<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE {whereKey} =? ", whereValue).ToList();
        }

        /// <summary>
        /// Returns true if and only if there are objects of the given
        /// type currently stored in the database.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Exists<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First != null;
        }

        /// <summary>
        /// Returns true if and only if there is an object in the database
        /// that satisfies the given where condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereKey"></param>
        /// <param name="whereValue"></param>
        /// <returns></returns>
        public static bool Exists<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE {whereKey} =? ", whereValue).First != null;
        }

        /// <summary>
        /// Returns true if and only if there is an object in the database
        /// that satisfies the given where conditions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whKey1"></param>
        /// <param name="whValue1"></param>
        /// <param name="whKey2"></param>
        /// <param name="whValue2"></param>
        /// <returns></returns>
        public static bool Exists<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1} =? AND t.{whKey2} =? ", whValue1, whValue2).First != null;
        }

        /// <summary>
        /// Fetches an object from the database.
        /// Equivalent to SQL: SELECT TOP(1) * FROM T WHERE key = value
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <param name="whereKey">The key used in the WHERE clause</param>
        /// <param name="whereValue">The value used in the WHERE clause</param>
        /// <returns>The first database entity from the specified type that matches the value on the specified key</returns>
        public static T Get<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whereKey} =? ", whereValue).First;
        }

        /// <summary>
        /// Fetches an object from the database.
        /// Equivalent to SQL: SELECT TOP(1) * FROM T WHERE [all key-value-pairs specified in values]
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <returns>The first database entity from the specified type that matches the given key-value-pairs</returns>
        public static T Get<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1} =? AND t.{whKey2} =? ", whValue1, whValue2).First;
        }

        /// <summary>
        /// Fetches an object from the database.
        /// Equivalent to SQL: SELECT TOP(1) * FROM T WHERE [all key-value-pairs specified in values]
        /// </summary>
        /// <typeparam name="T">The database entity to fetch</typeparam>
        /// <param name="whereKeyValuePairs">The key-value-pairs to use in the WHERE statement</param>
        /// <returns>The first database entity from the specified type that matches the given key-value-pairs</returns>
        public static T Get<T>(Dictionary<string, object> whereKeyValuePairs) where T : class
        {
            if (whereKeyValuePairs == null || !whereKeyValuePairs.Any()) return null;
            var whereKeys = string.Join(" AND ", whereKeyValuePairs.Select(pair => $"t.{pair.Key} =?"));
            var whereValues = whereKeyValuePairs.Values.ToArray();
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE {whereKeys}", whereValues).First();
        }

        /// <summary>
        /// Gets an IEnumerable of all the distinct values currently present in
        /// a table column.
        /// </summary>
        /// <typeparam name="TTable">The table to scan</typeparam>
        /// <typeparam name="TValue">The data type for values in the column to scan</typeparam>
        /// <param name="key">The column to scan for distinct values</param>
        /// <returns>An IEnumerable for the TValue type</returns>
        public static IEnumerable<TValue> Domain<TTable, TValue>(string key) where TTable : class
        {
            return Db.SQL<TValue>($"SELECT t.{key} from {typeof(TTable).FullName} t").Distinct();
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Tries to create an index in the database.
        /// </summary>
        /// <param name="columns">The names of the columns included in the index</param>
        public static void CreateIndex<T>(params string[] columns)
        {
            var nameHead = typeof(T).Name;
            var nameTail = string.Join("_", columns);
            try
            {
                Db.SQL($"CREATE INDEX {nameHead}_{nameTail} ON {typeof(T).FullName} ({string.Join(",", columns)})");
            }
            catch (DbException dbe)
            {
            }
            catch (Exception e)
            {
            }
        }

        #endregion
    }
}