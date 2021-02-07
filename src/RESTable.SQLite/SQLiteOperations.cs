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
        public static Task<IEnumerable<T>> Select(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            request.Conditions = post;
            var entities = SQLite<T>.Select(
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            );
            return Task.FromResult(entities);
        }

        public static async Task<int> Insert(IRequest<T> request)
        {
            return await SQLite<T>.Insert(await request.GetInputEntities());
        }

        public static async Task<int> Update(IRequest<T> request)
        {
            return await SQLite<T>.Update((await request.GetInputEntities()).ToList());
        }

        public static async Task<int> Delete(IRequest<T> request)
        {
            return await SQLite<T>.Delete((await request.GetInputEntities()).ToList());
        }

        public static async Task<long> Count(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            if (post.Any())
            {
                long count = SQLite<T>
                    .Select(sql.ToSQLiteWhereClause())
                    .Where(post)
                    .Count();
                return count;
            }
            return await SQLite<T>.Count(sql.ToSQLiteWhereClause());
        }

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.SQLColumnNames.Contains(condition.Term.First.Name);
        }
    }
}