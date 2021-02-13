﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.SQLite.Meta;
using RESTable.Linq;

namespace RESTable.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        public static IAsyncEnumerable<T> Select(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            request.Conditions = post;
            return SQLite<T>.Select(
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            );
        }

        public static async Task<int> Insert(IRequest<T> request)
        {
            return await SQLite<T>.Insert(request.GetInputEntitiesAsync());
        }

        public static async Task<int> Update(IRequest<T> request)
        {
            return await SQLite<T>.Update(request.GetInputEntitiesAsync());
        }

        public static async Task<int> Delete(IRequest<T> request)
        {
            return await SQLite<T>.Delete(request.GetInputEntitiesAsync());
        }

        public static async Task<long> Count(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            if (post.Any())
            {
                return await SQLite<T>
                    .Select(sql.ToSQLiteWhereClause())
                    .Where(post)
                    .CountAsync();
            }
            return await SQLite<T>.Count(sql.ToSQLiteWhereClause());
        }

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.SQLColumnNames.Contains(condition.Term.First.Name);
        }
    }
}