using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Sqlite.Meta;

namespace RESTable.Sqlite;

internal class EntityEnumerator<T> : IAsyncEnumerator<T> where T : SqliteTable
{
    private static readonly ParameterlessConstructor<T> ParameterlessConstructor;

    static EntityEnumerator()
    {
        ParameterlessConstructor = typeof(T).MakeParameterlessConstructor<T>() ??
                                   throw new InvalidOperationException($"Could not create constructor for type '{typeof(T).GetRESTableTypeName()}'");
    }

    internal EntityEnumerator(TableMapping tableMapping, string sql, bool onlyRowId, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        Sql = sql;
        OnlyRowId = onlyRowId;
        Current = null!;
        TransactMappings = tableMapping.TransactMappings;
    }

    private SQLiteConnection? Connection { get; set; }
    private SQLiteCommand? Command { get; set; }
    private DbDataReader? Reader { get; set; }
    private string Sql { get; }
    private bool OnlyRowId { get; }
    private CancellationToken CancellationToken { get; }
    private long CurrentRowId { get; set; }
    private ColumnMapping[] TransactMappings { get; }

    public async ValueTask DisposeAsync()
    {
        if (Reader is null)
            return;
        await Command!.DisposeAsync().ConfigureAwait(false);
        await Reader.DisposeAsync().ConfigureAwait(false);
        await Connection!.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (Reader is null)
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = connection.CreateCommand();
            command.CommandText = Sql;
            var reader = await command.ExecuteReaderAsync(CancellationToken).ConfigureAwait(false);
            Connection = connection;
            Command = command;
            Reader = reader;
        }
        var read = await Reader!.ReadAsync(CancellationToken).ConfigureAwait(false);
        if (!read) return false;
        CurrentRowId = await Reader.GetFieldValueAsync<long>(0, CancellationToken).ConfigureAwait(false);
        Current = await MakeEntity(Reader).ConfigureAwait(false);
        return true;
    }

    public T Current { get; private set; }

    private async ValueTask<T> MakeEntity(IDataRecord record)
    {
        var entity = ParameterlessConstructor();
        entity.RowId = CurrentRowId;
        if (!OnlyRowId)
            for (var index = 0; index < TransactMappings.Length; index++)
            {
                var column = TransactMappings[index];
                if (column.ClrProperty.Set is not Setter setter)
                    continue;
                var value = record[column.SqlColumn.Name];
                if (value is not DBNull)
                    await setter.Invoke(entity, value).ConfigureAwait(false);
                else if (!column.ClrProperty.IsDeclared) await setter.Invoke(entity, null).ConfigureAwait(false);
            }
        await entity._OnSelect().ConfigureAwait(false);
        return entity;
    }
}