using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace RESTable.SQLite
{
    internal static class Database
    {
        internal static async Task<int> QueryAsync(string sql)
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            await using (connection.ConfigureAwait(false))
            {
                var command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandText = sql;
                    return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        internal static async Task QueryAsync(string sql, Action<DbDataReader> rowAction) => await QueryAsync(sql, reader =>
        {
            rowAction(reader);
            return default;
        }).ConfigureAwait(false);

        internal static async Task QueryAsync(string sql, Func<DbDataReader, ValueTask> rowTask)
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            await using (connection.ConfigureAwait(false))
            {
                var command = new SQLiteCommand(sql, connection);
                await using (command.ConfigureAwait(false))
                {
                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync().ConfigureAwait(false))
                            await rowTask(reader).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}