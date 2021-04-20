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
            await using var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        internal static async Task QueryAsync(string sql, Action<DbDataReader> rowAction) => await QueryAsync(sql, reader =>
        {
            rowAction(reader);
            return default;
        }).ConfigureAwait(false);

        internal static async Task QueryAsync(string sql, Func<DbDataReader, ValueTask> rowTask)
        {
            await using var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            await using var command = new SQLiteCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
                await rowTask(reader).ConfigureAwait(false);
        }

        internal static async Task<int> TransactAsync(Func<SQLiteCommand, Task<int>> callback)
        {
            return await TransactAsync((_, command) => callback(command)).ConfigureAwait(false);
        }

        internal static async Task<int> TransactAsync(Func<SQLiteConnection, SQLiteCommand, Task<int>> callback)
        {
            await using var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            await using var command = connection.CreateCommand();
            await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                var result = await callback(connection, command).ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}