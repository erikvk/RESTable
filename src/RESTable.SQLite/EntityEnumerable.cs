using System.Collections.Generic;
using System.Threading;

namespace RESTable.Sqlite;

internal class EntityEnumerable<T> : IAsyncEnumerable<T> where T : SqliteTable
{
    internal EntityEnumerable(TableMapping tableMapping, string sql, bool onlyRowId)
    {
        Sql = sql;
        OnlyRowId = onlyRowId;
        TableMapping = tableMapping;
    }

    internal string Sql { get; }
    internal bool OnlyRowId { get; }
    internal TableMapping TableMapping { get; }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return new EntityEnumerator<T>(TableMapping, Sql, OnlyRowId, cancellationToken);
    }
}