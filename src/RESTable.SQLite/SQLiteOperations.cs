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
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            );
        }

        public static async Task<int> InsertAsync(IRequest<T> request)
        {
            return await SQLite<T>.Insert(request.GetInputEntitiesAsync()).ConfigureAwait(false);
        }

        public static async Task<int> UpdateAsync(IRequest<T> request)
        {
            return await SQLite<T>.Update(request.GetInputEntitiesAsync()).ConfigureAwait(false);
        }

        public static async Task<int> DeleteAsync(IRequest<T> request)
        {
            return await SQLite<T>.Delete(request.GetInputEntitiesAsync()).ConfigureAwait(false);
        }

        public static async Task<long> CountAsync(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            if (post.Any())
            {
                return await SQLite<T>
                    .Select(sql.ToSQLiteWhereClause())
                    .Where(post)
                    .CountAsync().ConfigureAwait(false);
            }
            return await SQLite<T>.Count(sql.ToSQLiteWhereClause()).ConfigureAwait(false);
        }

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.SQLColumnNames.Contains(condition.Term.First.Name);
        }
    }
}