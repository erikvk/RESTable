using System;
using System.Collections.Generic;
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
        private static readonly Constructor<T> Constructor = typeof(T).MakeStaticConstructor<T>();

        private SQLiteConnection Connection { get; set; }
        private SQLiteCommand Command { get; set; }
        private DbDataReader Reader { get; set; }
        private string Sql { get; }
        private bool OnlyRowId { get; }
        private CancellationToken CancellationToken { get; }
        private long CurrentRowId { get; set; }

        public async ValueTask DisposeAsync()
        {
            await Command.DisposeAsync().ConfigureAwait(false);
            await Reader.DisposeAsync().ConfigureAwait(false);
            await Connection.DisposeAsync().ConfigureAwait(false);
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            CancellationToken.ThrowIfCancellationRequested();
            if (Reader == null)
            {
                Connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn();
                Command = Connection.CreateCommand();;
                Command.CommandText = Sql;
                Reader ??= await Command.ExecuteReaderAsync(CancellationToken).ConfigureAwait(false);
            }
            var read = await Reader.ReadAsync(CancellationToken).ConfigureAwait(false);
            if (!read) return false;
            CurrentRowId = await Reader.GetFieldValueAsync<long>(0, CancellationToken).ConfigureAwait(false);
            return true;
        }

        internal EntityEnumerator(string sql, bool onlyRowId, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Sql = sql;
            OnlyRowId = onlyRowId;
        }

        public T Current => MakeEntity();

        private T MakeEntity()
        {
            var entity = Constructor();
            entity.RowId = CurrentRowId;
            if (!OnlyRowId)
            {
                foreach (var column in TableMapping<T>.TransactMappings)
                {
                    var value = Reader[column.SQLColumn.Name];
                    if (value is not DBNull)
                        column.CLRProperty.Set?.Invoke(entity, value);
                    else if (!column.CLRProperty.IsDeclared)
                        column.CLRProperty.Set?.Invoke(entity, null);
                }
            }
            entity._OnSelect();
            return entity;
        }
    }
}