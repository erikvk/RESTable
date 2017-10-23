using System.Linq;
using System.Text;
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
            return SQLiteDb.Query<T>(
                $"SELECT RowId,* FROM {request.Resource.GetSQLiteTableName()} {dbConditions.ToSQLiteWhereClause()}",
                request.Resource.GetColumns()
            ).Where(postConditions);
        };

        public static Inserter<T> Insert => (entities, request) =>
        {
            var columns = request.Resource.GetColumns().Values;
            var sqlStub = $"INSERT INTO {request.Resource.GetSQLiteTableName()} VALUES ";
            var stringBuilder = new StringBuilder(sqlStub);
            foreach (var entity in entities)
            {
                stringBuilder.Append("(");
                stringBuilder.Append(entity.ToSQLiteInsertInto(columns));
                stringBuilder.Append("),");
            }
            var sql = stringBuilder.ToString().TrimEnd(',');
            return SQLiteDb.Query(sql);
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