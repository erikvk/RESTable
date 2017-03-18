using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTar
{
    public static class DbTools
    {
        private static readonly MethodInfo SQL = typeof(Db).GetMethods()
            .First(m => m.Name == "SQL" && m.IsGenericMethod);

        public static IEnumerable<dynamic> Select(IRequest request)
        {
            var whereClause = request.Conditions?.ToWhereClause();
            var sql = $"SELECT t FROM {request.Resource.TargetType.FullName} t {whereClause?.stringPart} {request.OrderBy?.SQL}";
            dynamic entities;
            var generic = SQL.MakeGenericMethod(request.Resource.TargetType);
            if (request.Limit < 1)
                entities = generic.Invoke(null, new object[] {sql, whereClause?.valuesPart});
            else
                entities = Enumerable.ToList(Enumerable.Take
                (
                    (dynamic) generic.Invoke(null, new object[] {sql, whereClause?.valuesPart}),
                    request.Limit
                ));
            return entities;
        }

        public static IEnumerable<T> StaticSelect<T>(IRequest request)
            => StaticSelect<T>(request.Conditions, request.Limit, request.OrderBy);

        public static IEnumerable<T> StaticSelect<T>(IList<Condition> conditions, int limit, OrderBy orderBy)
        {
            var whereClause = conditions?.ToWhereClause();
            var sql = $"SELECT t FROM {typeof(T).FullName} t {whereClause?.stringPart} {orderBy?.SQL}";
            if (limit < 1)
                return Db.SQL<T>(sql, whereClause?.valuesPart);
            return Db.SQL<T>(sql, whereClause?.valuesPart).Take(limit).ToList();
        }
    }


    /// <summary>
    /// This class provides static methods for database queries in the DRTB system.
    /// </summary>
    internal static class DB
    {
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
                             $"WHERE t.{whereKey.Fnuttify()} =? ", whereValue).ToList();
        }

        public static bool Exists<T>() where T : class
        {
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t").First != null;
        }

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        public static bool Exists<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whereKey.Fnuttify()} =? ", whereValue).First != null;
        }

        public static bool Exists<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return false;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1.Fnuttify()} =? AND t.{whKey2.Fnuttify()} =? ", whValue1, whValue2).First != null;
        }

        public static T Get<T>(string whereKey, object whereValue) where T : class
        {
            if (string.IsNullOrEmpty(whereKey)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE t.{whereKey.Fnuttify()} =? ", whereValue).First;
        }

        public static T Get<T>(string whKey1, object whValue1, string whKey2, object whValue2) where T : class
        {
            if (string.IsNullOrEmpty(whKey1) || string.IsNullOrEmpty(whKey2)) return null;
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t " +
                             $"WHERE t.{whKey1.Fnuttify()} =? AND t.{whKey2.Fnuttify()} =? ", whValue1, whValue2).First;
        }

        public static T Get<T>(Dictionary<string, object> whereKeyValuePairs) where T : class
        {
            if (whereKeyValuePairs == null || !whereKeyValuePairs.Any()) return null;
            var whereKeys = string.Join(" AND ", whereKeyValuePairs.Select(pair => $"t.{pair.Key.Fnuttify()} =?"));
            var whereValues = whereKeyValuePairs.Values.ToArray();
            return Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t WHERE {whereKeys}", whereValues).First();
        }

        #endregion
    }
}