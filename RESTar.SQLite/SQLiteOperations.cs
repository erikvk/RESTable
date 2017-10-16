using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.SQLite
{
    internal static class SQLiteOperations<T> where T : class, ISQLiteTable
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

            SQLiteDb.Select(rawQuery, row =>
            {
                var entity = Activator.CreateInstance<T>();
                entity.RowId = row.GetInt64(0);
                foreach (var column in columns)
                {
                    var value = row[column.Key];
                    if (value != DBNull.Value)
                    {
                        var targetType = column.Value.Type;
                        if (targetType.IsNullable(out var baseType))
                            targetType = baseType;
                        column.Value.SetValue(entity, Convert.ChangeType(value, targetType));
                    }
                }
                results.Add(entity);
            });

            return results.Where(postConditions);
        };

        public static Inserter<T> Insert => (entities, request) =>
        {
            var columns = request.Resource.GetColumns();
            var count = 0;
            var sqlStub = $"INSERT INTO {request.Resource.GetSQLiteTableName()} VALUES (";
            foreach (var entity in entities)
                SQLiteDb.Query($"{sqlStub}{entity.ToSQLiteInsertInto(columns.Values)})", c => count += c.ExecuteNonQuery());
            return count;
        };

        public static Updater<T> Update => (e, r) => e.Count();

        public static Deleter<T> Delete => (entities, request) =>
        {
            var count = 0;
            var sqlstub = $"DELETE FROM {request.Resource.GetSQLiteTableName()} WHERE RowId=";
            foreach (var entity in entities)
                SQLiteDb.Query(sqlstub + entity.RowId, command => count += command.ExecuteNonQuery());
            return count;
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
            SQLiteDb.Select($"SELECT COUNT(*) FROM {request.Resource.GetSQLiteTableName()} " +
                            dbConditions.ToSQLiteWhereClause(), row => count = row.GetInt64(0));
            return count;
        };
    }
}