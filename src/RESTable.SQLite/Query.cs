﻿using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Sqlite;

public class Query
{
    public Query(string sql)
    {
        ConnectionString = Settings.ConnectionString;
        Sql = sql;
    }

    private string ConnectionString { get; }
    private string Sql { get; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = new())
    {
        var connection = new SQLiteConnection(ConnectionString).OpenAndReturn();
        await using (connection.ConfigureAwait(false))
        {
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = Sql;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async IAsyncEnumerable<DbDataReader> GetRows([EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        var connection = new SQLiteConnection(ConnectionString).OpenAndReturn();
        await using (connection.ConfigureAwait(false))
        {
            var command = connection.CreateCommand();
            command.CommandText = Sql;
            await using (command.ConfigureAwait(false))
            {
                var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) yield return reader;
                }
            }
        }
    }
}
