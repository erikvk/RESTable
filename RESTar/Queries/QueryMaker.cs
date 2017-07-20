using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Queries
{
    /// <summary>
    /// A static class for query generation
    /// </summary>
    public static class QueryMaker<T> where T : class
    {
        /// <summary>
        /// Creates a SELECT SQL query for the given type, list of conditions and optional meta-conditions 
        /// </summary>
        public static (string SQL, object[] values) SELECT(IEnumerable<Condition<T>> conditions,
            MetaConditions metaConditions = null)
        {
            if (!typeof(T).IsStarcounter() || typeof(T).IsDDictionary())
                throw new ArgumentException("Can only get SQL for static Starcounter resources.");
            var whereClause = conditions.MakeWhereClause();
            return ($"SELECT t FROM {typeof(T).FullName} t " +
                    $"{whereClause.WhereString} " +
                    $"{metaConditions?.OrderBy?.SQL} " +
                    $"{metaConditions?.Limit.SQL}", whereClause.Values);
        }
    }
}