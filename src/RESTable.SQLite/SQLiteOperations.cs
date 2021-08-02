using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.SQLite.Meta;
using RESTable.Linq;

namespace RESTable.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        public static IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            request.Conditions = post;
            return SQLite<T>.Select
            (
                request.Context,
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any(),
                cancellationToken: cancellationToken
            );
        }

        public static IAsyncEnumerable<T> InsertAsync(IRequest<T> request, CancellationToken cancellationToken)
        {
            return SQLite<T>.Insert(request.Context, request.GetInputEntitiesAsync(), cancellationToken);
        }

        public static IAsyncEnumerable<T> UpdateAsync(IRequest<T> request, CancellationToken cancellationToken)
        {
            return SQLite<T>.Update(request.Context, request.GetInputEntitiesAsync(), cancellationToken);
        }

        public static async ValueTask<int> DeleteAsync(IRequest<T> request, CancellationToken cancellationToken)
        {
            return await SQLite<T>.Delete(request.Context, request.GetInputEntitiesAsync(), cancellationToken).ConfigureAwait(false);
        }

        public static async ValueTask<long> CountAsync(IRequest<T> request, CancellationToken cancellationToken)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            if (post.Any())
            {
                return await SQLite<T>
                    .Select(request.Context, sql.ToSQLiteWhereClause(), cancellationToken: cancellationToken)
                    .Where(post)
                    .CountAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            return await SQLite<T>.Count(request.Context, sql.ToSQLiteWhereClause()).ConfigureAwait(false);
        }

        private static bool IsSQLiteQueryable(ICondition condition) => condition.Term.Count == 1 &&
                                                                       condition.Term.First?.Name is string firstName &&
                                                                       TableMapping<T>.SQLColumnNames.Contains(firstName);
    }
}