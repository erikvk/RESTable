﻿using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace RESTable.SQLite
{
    public class Query
    {
        private string ConnectionString { get; }
        private string Sql { get; }

        public Query(string sql)
        {
            ConnectionString = Settings.ConnectionString;
            Sql = sql;
        }

        public async Task Execute()
        {
            var connection = new SQLiteConnection(ConnectionString).OpenAndReturn();
            await using (connection.ConfigureAwait(false))
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = Sql;
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        public async IAsyncEnumerable<DbDataReader> GetRows()
        {
            var connection = new SQLiteConnection(ConnectionString).OpenAndReturn();
            await using (connection.ConfigureAwait(false))
            {
                var command = connection.CreateCommand();
                command.CommandText = Sql;
                await using (command.ConfigureAwait(false))
                {
                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            yield return reader;
                        }
                    }
                }
            }
        }
    }
}