using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.SQLite.Meta;
using RESTable.Linq;

namespace RESTable.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        public static IAsyncEnumerable<T> SelectAsync(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            request.Conditions = post;
            return SQLite<T>.Select(
                request.Context,
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            );
        }

        public static IAsyncEnumerable<T> InsertAsync(IRequest<T> request)
        {
            return SQLite<T>.Insert(request.Context, request.GetInputEntitiesAsync());
        }

        public static IAsyncEnumerable<T> UpdateAsync(IRequest<T> request)
        {
            return SQLite<T>.Update(request.Context, request.GetInputEntitiesAsync());
        }

        public static async ValueTask<int> DeleteAsync(IRequest<T> request)
        {
            return await SQLite<T>.Delete(request.Context, request.GetInputEntitiesAsync()).ConfigureAwait(false);
        }

        public static async ValueTask<long> CountAsync(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            if (post.Any())
            {
                return await SQLite<T>
                    .Select(request.Context, sql.ToSQLiteWhereClause())
                    .Where(post)
                    .CountAsync()
                    .ConfigureAwait(false);
            }
            return await SQLite<T>.Count(request.Context, sql.ToSQLiteWhereClause()).ConfigureAwait(false);
        }

        private static bool IsSQLiteQueryable(ICondition condition) => condition.Term.Count == 1 &&
                                                                       condition.Term.First?.Name is string firstName &&
                                                                       TableMapping<T>.SQLColumnNames.Contains(firstName);
    }
}