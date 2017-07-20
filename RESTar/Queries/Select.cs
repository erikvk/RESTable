using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar.Queries
{
    /// <summary>
    /// This class provides static methods for database queries
    /// </summary>
    public static class Select<T> where T : class
    {
        private static string _SELECT => $"SELECT t FROM {typeof(T).FullName} t";
        private static string _COUNT => $"SELECT COUNT(t) FROM {typeof(T).FullName} t";

        #region Get methods

        /// <summary>
        /// Gets an object of a given type from object number (ObjectNo).
        /// </summary>
        /// <typeparam name="T">The type of the object to get</typeparam>
        /// <param name="objectNo">The current object number to search for</param>
        /// <returns>The object with the given object number</returns>
        public static T ByObjectNo(ulong objectNo) => DbHelper.FromID(objectNo) as T;

        /// <summary>
        /// Gets an object of a given type from object ID (ObjectID).
        /// </summary>
        /// <typeparam name="T">The type of the object to get</typeparam>
        /// <param name="objectId">The current object ID to search for</param>
        /// <returns>The object with the given object ID</returns>
        public static T ByObjectId(string objectId) => DbHelper.FromID(DbHelper.Base64DecodeObjectID(objectId)) as T;

        /// <summary>
        /// Gets the first entity from a given database table
        /// </summary>
        public static T First => All.First;

        /// <summary>
        /// Gets all entities from a given database table
        /// </summary>
        public static QueryResultRows<T> All => Db.SQL<T>(_SELECT);

        /// <summary>
        /// Returns true if there are no entities of this type in the database
        /// </summary>
        public static bool Any => All.Any();

        /// <summary>
        /// Gets all the entities in the database satisfying the given conditions.
        /// </summary>
        public static QueryResultRows<T> Where(params Condition<T>[] conditions)
        {
            var whereClause = conditions.MakeWhereClause();
            return Db.SQL<T>($"{_SELECT} {whereClause.WhereString}", whereClause.Values);
        }

        /// <summary>
        /// Gets all the entities in the database satisfying the given conditions.
        /// </summary>
        public static QueryResultRows<T> Where(IEnumerable<Condition<T>> conditions)
        {
            var whereClause = conditions.MakeWhereClause();
            return Db.SQL<T>($"{_SELECT} {whereClause.WhereString}", whereClause.Values);
        }

        /// <summary>
        /// Counts all entities in this database table
        /// </summary>
        public static long Count() => Db.SQL<long>(_COUNT).First;

        /// <summary>
        /// Counts all entities in this database table that satisfy the given conditions.
        /// </summary>
        public static long Count(params Condition<T>[] conditions)
        {
            var whereClause = conditions.MakeWhereClause();
            var query = $"{_COUNT} {whereClause.WhereString}";
            return Db.SQL<long>(query, whereClause.Values).First;
        }

        /// <summary>
        /// Counts all entities in this database table that satisfy the given conditions.
        /// </summary>
        public static long Count(IEnumerable<Condition<T>> conditions)
        {
            var whereClause = conditions.MakeWhereClause();
            var query = $"{_COUNT} {whereClause.WhereString}";
            return Db.SQL<long>(query, whereClause.Values).First;
        }

        #endregion
    }
}