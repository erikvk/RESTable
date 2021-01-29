﻿using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;
using RESTable.SQLite.Meta;
using RESTable.Linq;

namespace RESTable.SQLite
{
    internal static class SQLiteOperations<T> where T : SQLiteTable
    {
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            request.Conditions = post;
            return SQLite<T>.Select(
                where: sql.ToSQLiteWhereClause(),
                onlyRowId: request.Method == Method.DELETE && !request.Conditions.Any()
            );
        }

        public static int Insert(IRequest<T> request)
        {
            return SQLite<T>.Insert(request.GetInputEntities());
        }

        public static int Update(IRequest<T> request)
        {
            return SQLite<T>.Update(request.GetInputEntities().ToList());
        }

        public static int Delete(IRequest<T> request)
        {
            return SQLite<T>.Delete(request.GetInputEntities().ToList());
        }

        public static long Count(IRequest<T> request)
        {
            var (sql, post) = request.Conditions.Split(IsSQLiteQueryable);
            return post.Any()
                ? SQLite<T>
                    .Select(sql.ToSQLiteWhereClause())
                    .Where(post)
                    .Count()
                : SQLite<T>.Count(sql.ToSQLiteWhereClause());
        }

        private static bool IsSQLiteQueryable(ICondition condition)
        {
            return condition.Term.Count == 1 && TableMapping<T>.SQLColumnNames.Contains(condition.Term.First.Name);
        }
    }
}