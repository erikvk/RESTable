using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Admin;
using RESTable.Sqlite.Meta;

namespace RESTable.Sqlite
{
    /// <summary>
    /// Helper class for accessing RESTable.SQLite tables
    /// </summary>
    /// <typeparam name="T">The SqliteTable class to bind SQL operations to</typeparam>
    public static class Sqlite<T> where T : SqliteTable
    {
        private const string RowIdParameter = "@rowId";

        /// <summary>
        /// Selects entities in the Sqlite database using the RESTable.Sqlite O/RM mapping 
        /// facilities. Returns an IAsyncEnumerable of the provided resource type.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL squery to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <param name="onlyRowId">Populates only RowIds for the resulting entities</param>
        /// <returns></returns>
        public static IAsyncEnumerable<T> Select(string? where = null, bool onlyRowId = false)
        {
            var tableMapping = TableMapping<T>.Get;
            var enumerable = new EntityEnumerable<T>
            (
                tableMapping: tableMapping,
                sql: $"SELECT RowId,* FROM {TableMapping<T>.TableName} {where}",
                onlyRowId: onlyRowId
            );
            return enumerable;
        }

        /// <summary>
        /// Inserts a range of SqliteTable entities into the given Sqlite database table and returns the number of inserted entities
        /// </summary>
        public static ValueTask<long> Insert(params T[] entities) => Insert(entities.ToAsyncEnumerable()).LongCountAsync();

        /// <summary>
        /// Inserts an IEnumerable of SqliteTable entities into the appropriate SQLite database
        /// table and returns the inserted rows.
        /// </summary>
        public static async IAsyncEnumerable<T> Insert(IAsyncEnumerable<T> entities, [EnumeratorCancellation] CancellationToken cancellationToken = new())
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = connection.CreateCommand();
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (connection.ConfigureAwait(false))
            await using (command.ConfigureAwait(false))
            await using (transaction.ConfigureAwait(false))
            {
                var (name, columns, param, mappings) = TableMapping<T>.InsertSpec;
                for (var i = 0; i < mappings.Length; i++)
                    command.Parameters.Add(param[i], mappings[i].SqlColumn.DbType.GetValueOrDefault());
                command.CommandText = $"INSERT INTO {name} ({columns}) VALUES ({string.Join(", ", param)})";

                await foreach (var entity in entities.ConfigureAwait(false))
                {
                    await entity._OnInsert().ConfigureAwait(false);
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        var getter = mappings[i].ClrProperty.Getter;
                        object? propertyValue = null;
                        if (getter is not null)
                            propertyValue = await getter(entity).ConfigureAwait(false);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    await QueryConsole.Publish(command.CommandText, cancellationToken).ConfigureAwait(false);
                    var insertedCount = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    entity.RowId = connection.LastInsertRowId;
                    if (insertedCount == 1)
                        yield return entity;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates a range of SqliteTable entities in the given Sqlite database table and returns the number of updated entities
        /// </summary>
        public static ValueTask<long> Update(params T[] updatedEntities) => Update(updatedEntities.ToAsyncEnumerable()).LongCountAsync();

        /// <summary>
        /// Updates a range of SqliteTable entities in the given Sqlite database table and returns an enumeration of updated entities
        /// </summary>
        public static async IAsyncEnumerable<T> Update(IAsyncEnumerable<T> updatedEntities, [EnumeratorCancellation] CancellationToken cancellationToken = new())
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = connection.CreateCommand();
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (connection.ConfigureAwait(false))
            await using (command.ConfigureAwait(false))
            await using (transaction.ConfigureAwait(false))
            {
                var (name, set, param, mappings) = TableMapping<T>.UpdateSpec;
                for (var i = 0; i < mappings.Length; i++)
                    command.Parameters.Add(param[i], mappings[i].SqlColumn.DbType.GetValueOrDefault());
                command.Parameters.Add(RowIdParameter, DbType.Int64);
                command.CommandText = $"UPDATE {name} SET {set} WHERE RowId = {RowIdParameter}";

                await foreach (var entity in updatedEntities.ConfigureAwait(false))
                {
                    await entity._OnUpdate().ConfigureAwait(false);
                    command.Parameters[RowIdParameter].Value = entity.RowId;
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        var getter = mappings[i].ClrProperty.Getter;
                        object? propertyValue = null;
                        if (getter is not null)
                            propertyValue = await getter(entity).ConfigureAwait(false);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    await QueryConsole.Publish(command.CommandText, cancellationToken).ConfigureAwait(false);
                    var updatedCount = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (updatedCount == 1)
                        yield return entity;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a range of SqliteTable entities in the given Sqlite database table and returns the number of deleted entities
        /// </summary>
        public static ValueTask<long> Delete(params T[] toDelete) => Delete(toDelete.ToAsyncEnumerable());

        /// <summary>
        /// Deletes the corresponding Sqlite database table rows for a given enumeration of entities, and returns the number of database rows affected.
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async ValueTask<long> Delete(IAsyncEnumerable<T> entities, CancellationToken cancellationToken = new())
        {
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = connection.CreateCommand();
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await using (connection.ConfigureAwait(false))
            await using (command.ConfigureAwait(false))
            await using (transaction.ConfigureAwait(false))
            {
                command.CommandText = $"DELETE FROM {TableMapping<T>.TableName} WHERE RowId = {RowIdParameter}";
                command.Parameters.Add(RowIdParameter, DbType.Int64);

                var count = 0;
                await foreach (var entity in entities.ConfigureAwait(false))
                {
                    await entity._OnDelete().ConfigureAwait(false);
                    command.Parameters[RowIdParameter].Value = entity.RowId;
                    await QueryConsole.Publish(command.CommandText, cancellationToken).ConfigureAwait(false);
                    count += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return count;
            }
        }

        /// <summary>
        /// Counts all rows in the Sqlite database where a certain where clause is true.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT COUNT(*) FROM {type} " in the actual query</param>
        /// <returns></returns>
        public static async ValueTask<long> Count(string? where = null, CancellationToken cancellationToken = new())
        {
            var sql = $"SELECT COUNT(RowId) FROM {TableMapping<T>.TableName} {where}";
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = new SQLiteCommand(sql, connection);

            await using (connection.ConfigureAwait(false))
            await using (command.ConfigureAwait(false))
            {
                await QueryConsole.Publish(command.CommandText, cancellationToken).ConfigureAwait(false);
                var result = (long?) await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result.GetValueOrDefault();
            }
        }
    }
}