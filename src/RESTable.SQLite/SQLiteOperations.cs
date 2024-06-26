﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Linq;
using RESTable.Requests;
using RESTable.Sqlite.Meta;

namespace RESTable.Sqlite;

internal static class SqliteOperations<T> where T : SqliteTable
{
    public static IAsyncEnumerable<T> SelectAsync(IRequest<T> request)
    {
        var (sql, post) = request.Conditions.Split(IsSqliteQueryable);
        request.Conditions = post;
        return Sqlite<T>.Select
        (
            sql.ToSqliteWhereClause(),
            request.Method == Method.DELETE && !request.Conditions.Any()
        );
    }

    public static IAsyncEnumerable<T> InsertAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Sqlite<T>.Insert(request.GetInputEntitiesAsync(), cancellationToken);
    }

    public static IAsyncEnumerable<T> UpdateAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Sqlite<T>.Update(request.GetInputEntitiesAsync(), cancellationToken);
    }

    public static async ValueTask<long> DeleteAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return await Sqlite<T>.Delete(request.GetInputEntitiesAsync(), cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<long> CountAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        var (sql, post) = request.Conditions.Split(IsSqliteQueryable);
        if (post.Any())
            return await Sqlite<T>
                .Select(sql.ToSqliteWhereClause())
                .Where(post)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
        return await Sqlite<T>.Count(sql.ToSqliteWhereClause(), cancellationToken).ConfigureAwait(false);
    }

    private static bool IsSqliteQueryable(ICondition condition)
    {
        return condition.Term is { Count: 1, First.Name: string firstName } && TableMapping<T>.SqlColumnNames.Contains(firstName);
    }
}
