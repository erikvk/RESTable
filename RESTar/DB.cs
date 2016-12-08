using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar
{
    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
    /// </summary>
    public static class DB
    {
        internal static IEnumerable<dynamic> GetStatic(Type resource, WhereClause whereClause,
            int limit = -1, OrderBy orderBy = null)
        {
            var sql = $"SELECT t FROM {resource.FullName} t {whereClause?.stringPart} {orderBy?.SQL}";
            dynamic entities;

            var method = typeof(Db).GetMethods().First(m => m.Name == "SQL" && m.IsGenericMethod);
            var generic = method.MakeGenericMethod(resource);

            if (limit < 1)
                entities = generic.Invoke(null, new object[] {sql, whereClause?.valuesPart});
            else
                entities = Enumerable.Take((dynamic) generic.Invoke(null, new object[] {sql, whereClause?.valuesPart}),
                    limit);

            return entities;
        }

        #region Dynamic get

        //        private static IEnumerable<object> GetFromDb(Type resource, WhereClause whereClause,
        //            int limit = -1, OrderBy orderBy = null)
        //        {
        //            var sql = $"SELECT t FROM {resource.FullName} t {whereClause?.stringPart} {orderBy?.SQL}";
        //            IEnumerable<object> entities;
        //            if (limit < 1)
        //                entities = Db.SQL(sql, whereClause?.valuesPart);
        //            else if (limit == 1)
        //                entities = new[] {Db.SQL(sql, whereClause?.valuesPart).First};
        //            else entities = Db.SQL(sql, whereClause?.valuesPart).Take(limit);
        //            return entities;
        //        }

        #endregion

        #region Get methods

        public static long RowCount(string tableName)
        {
            return (long) Db.SQL($"SELECT COUNT(t) FROM {tableName} t").First;
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