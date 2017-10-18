using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        internal static Selector<T> Select => request =>
        {
            var (dbConditions, postConditions) = request.Conditions.Split(c =>
                c.Term.Count == 1 &&
                c.Term.First is StaticProperty stat &&
                stat.HasAttribute<ColumnAttribute>()
            );

            var rawQuery = $"SELECT RowId,* FROM {request.Resource.GetSQLiteTableName()} " +
                           dbConditions.ToSQLiteWhereClause();
            var columns = request.Resource.GetColumns();
            var results = new List<T>();

            SQLiteDb.Query(rawQuery, row =>
            {
                var entity = Activator.CreateInstance<T>();
                entity.RowId = row.GetInt64(0);
                columns.ForEach(column =>
                {
                    var value = row[column.Key];
                    if (value is DBNull) return;
                    column.Value.SetValue(entity, value);
                });
                results.Add(entity);
            });

            return results.Where(postConditions);
        };

        public static Inserter<T> Insert => (entities, request) =>
        {
            var columns = request.Resource.GetColumns().Values;
            var sqlStub = $"INSERT INTO {request.Resource.GetSQLiteTableName()} VALUES (";
            return entities.Sum(entity => SQLiteDb.Query($"{sqlStub}{entity.ToSQLiteInsertInto(columns)})"));
        };

        public static Updater<T> Update => (e, r) => e.Count();

        public static Deleter<T> Delete => (entities, request) =>
        {
            var sqlstub = $"DELETE FROM {request.Resource.GetSQLiteTableName()} WHERE RowId=";
            return entities.Sum(entity => SQLiteDb.Query(sqlstub + entity.RowId));
        };

        public static Counter<T> Count => request =>
        {
            var (dbConditions, postConditions) = request.Conditions.Split(c =>
                c.Term.Count == 1 &&
                c.Term.First is StaticProperty stat &&
                stat.HasAttribute<ColumnAttribute>()
            );

            if (postConditions.Any())
                return Select(request).Count();
            var count = 0L;
            SQLiteDb.Query($"SELECT COUNT(*) FROM {request.Resource.GetSQLiteTableName()} " +
                           dbConditions.ToSQLiteWhereClause(), row => count = row.GetInt64(0));
            return count;
        };
    }
}