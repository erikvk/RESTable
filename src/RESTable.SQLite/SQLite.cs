using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Admin;
using RESTable.Requests;
using RESTable.SQLite.Meta;

namespace RESTable.SQLite
{
    /// <summary>
    /// Helper class for accessing RESTable.SQLite tables
    /// </summary>
    /// <typeparam name="T">The SQLiteTable class to bind SQL operations to</typeparam>
    public static class SQLite<T> where T : SQLiteTable
    {
        private const string RowIdParameter = "@rowId";

        /// <summary>
        /// Selects entities in the SQLite database using the RESTable.SQLite O/RM mapping 
        /// facilities. Returns an IAsyncEnumerable of the provided resource type.
        /// </summary>
        /// <param name="where">The WHERE clause of the SQL squery to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <param name="onlyRowId">Populates only RowIds for the resulting entities</param>
        /// <returns></returns>
        public static IAsyncEnumerable<T> Select(string? where = null, bool onlyRowId = false)
        {
            var enumerable = new EntityEnumerable<T>
            (
                sql: $"SELECT RowId,* FROM {TableMapping<T>.TableName} {@where}",
                onlyRowId: onlyRowId
            );
            return enumerable;
        }

        /// <summary>
        /// Selects entities in the SQLite database using the RESTable.SQLite O/RM mapping 
        /// facilities. Returns an IEnumerable of the provided resource type.
        /// </summary>
        /// <param name="context">The context in which this call is made</param>
        /// <param name="where">The WHERE clause of the SQL squery to execute. Will be preceded 
        /// by "SELECT * FROM {type} " in the actual query</param>
        /// <param name="onlyRowId">Populates only RowIds for the resulting entities</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async IAsyncEnumerable<T> Select
        (
            RESTableContext? context,
            string? where = null,
            bool onlyRowId = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = new()
        )
        {
            var enumerable = (EntityEnumerable<T>) Select(where, onlyRowId);
            if (context is not null)
                await QueryConsole.Publish(context, enumerable.Sql, null).ConfigureAwait(false);
            await foreach (var item in enumerable.ConfigureAwait(false))
                yield return item;
        }

        /// <summary>
        /// Inserts a range of SQLiteTable entities into the appropriate SQLite database
        /// table and returns the number of rows affected.
        /// </summary>
        public static IAsyncEnumerable<T> Insert(params T[] entities) => Insert(null, entities.ToAsyncEnumerable());

        /// <summary>
        /// Inserts an IEnumerable of SQLiteTable entities into the appropriate SQLite database
        /// table and returns the inserted rows.
        /// </summary>
        public static async IAsyncEnumerable<T> Insert(RESTableContext? context, IAsyncEnumerable<T> entities, [EnumeratorCancellation] CancellationToken cancellationToken = new())
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
                    command.Parameters.Add(param[i], mappings[i].SQLColumn.DbType.GetValueOrDefault());
                command.CommandText = $"INSERT INTO {name} ({columns}) VALUES ({string.Join(", ", param)})";

                await foreach (var entity in entities.ConfigureAwait(false))
                {
                    await entity._OnInsert().ConfigureAwait(false);
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        var getter = mappings[i].CLRProperty.Getter;
                        object? propertyValue = null;
                        if (getter is not null)
                            propertyValue = await getter(entity).ConfigureAwait(false);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    if (context is not null)
                        await QueryConsole.Publish(context, command.CommandText, null).ConfigureAwait(false);
                    var insertedCount = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    entity.RowId = connection.LastInsertRowId;
                    if (insertedCount == 1)
                        yield return entity;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates the corresponding SQLite database table rows for a given IEnumerable 
        /// of updated entities and returns the number of rows affected.
        /// </summary>
        public static async IAsyncEnumerable<T> Update
        (
            RESTableContext? context,
            IAsyncEnumerable<T> updatedEntities,
            [EnumeratorCancellation] CancellationToken cancellationToken = new()
        )
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
                    command.Parameters.Add(param[i], mappings[i].SQLColumn.DbType.GetValueOrDefault());
                command.Parameters.Add(RowIdParameter, DbType.Int64);
                command.CommandText = $"UPDATE {name} SET {set} WHERE RowId = {RowIdParameter}";

                await foreach (var entity in updatedEntities.ConfigureAwait(false))
                {
                    await entity._OnUpdate().ConfigureAwait(false);
                    command.Parameters[RowIdParameter].Value = entity.RowId;
                    for (var i = 0; i < mappings.Length; i++)
                    {
                        var getter = mappings[i].CLRProperty.Getter;
                        object? propertyValue = null;
                        if (getter is not null)
                            propertyValue = await getter(entity).ConfigureAwait(false);
                        command.Parameters[param[i]].Value = propertyValue;
                    }
                    if (context is not null)
                        await QueryConsole.Publish(context, command.CommandText, null).ConfigureAwait(false);
                    var updatedCount = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    if (updatedCount == 1)
                        yield return entity;
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes the corresponding SQLite database table rows for a given IEnumerable 
        /// of entities, and returns the number of database rows affected.
        /// </summary>
        /// <param name="context">The context in which this call is made</param>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<int> Delete(RESTableContext? context, IAsyncEnumerable<T> entities, CancellationToken cancellationToken = new())
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
                    if (context is not null)
                        await QueryConsole.Publish(context, command.CommandText, null).ConfigureAwait(false);
                    count += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return count;
            }
        }

        /// <summary>
        /// Counts all rows in the SQLite database where a certain where clause is true.
        /// </summary>
        /// <param name="context">The context in which this call is made</param>
        /// <param name="where">The WHERE clause of the SQL query to execute. Will be preceded 
        /// by "SELECT COUNT(*) FROM {type} " in the actual query</param>
        /// <returns></returns>
        public static async Task<long> Count(RESTableContext? context, string? where = null)
        {
            var sql = $"SELECT COUNT(RowId) FROM {TableMapping<T>.TableName} {where}";
            var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
            var command = new SQLiteCommand(sql, connection);

            await using (connection.ConfigureAwait(false))
            await using (command.ConfigureAwait(false))
            {
                if (context is not null)
                    await QueryConsole.Publish(context, command.CommandText, null).ConfigureAwait(false);
                var result = (long?) await command.ExecuteScalarAsync().ConfigureAwait(false);
                return result.GetValueOrDefault();
            }
        }
    }
}