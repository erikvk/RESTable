using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.SQLite.Meta;

namespace RESTable.SQLite
{
    internal class EntityEnumerator<T> : IAsyncEnumerator<T> where T : SQLiteTable
    {
        private static readonly Constructor<T> Constructor;

        static EntityEnumerator()
        {
            Constructor = typeof(T).MakeStaticConstructor<T>() ?? throw new InvalidOperationException($"Could not create constructor for type '{typeof(T).GetRESTableTypeName()}'");
        }

        private SQLiteConnection? Connection { get; set; }
        private SQLiteCommand? Command { get; set; }
        private DbDataReader? Reader { get; set; }
        private string Sql { get; }
        private bool OnlyRowId { get; }
        private CancellationToken CancellationToken { get; }
        private long CurrentRowId { get; set; }

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

        internal EntityEnumerator(string sql, bool onlyRowId, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Sql = sql;
            OnlyRowId = onlyRowId;
            Current = null!;
        }


        public T Current { get; private set; }

        private async ValueTask<T> MakeEntity(IDataRecord record)
        {
            var entity = Constructor();
            entity.RowId = CurrentRowId;
            if (!OnlyRowId)
            {
                foreach (var column in TableMapping<T>.TransactMappings)
                {
                    if (column.CLRProperty.Set is not Setter setter)
                        continue;
                    var value = record[column.SQLColumn.Name];
                    if (value is not DBNull)
                    {
                        await setter.Invoke(entity, value).ConfigureAwait(false);
                    }
                    else if (!column.CLRProperty.IsDeclared)
                    {
                        await setter.Invoke(entity, null).ConfigureAwait(false);
                    }
                }
            }
            await entity._OnSelect().ConfigureAwait(false);
            return entity;
        }
    }
}