using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.SQLite.Meta;

namespace RESTable.SQLite
{
    internal class EntityEnumerator<T> : IAsyncEnumerator<T> where T : SQLiteTable
    {
        private static readonly Constructor<T> Constructor = typeof(T).MakeStaticConstructor<T>();
        private SQLiteDataReader Reader { get; set; }
        private SQLiteConnection Connection { get; }
        private SQLiteCommand Command { get; set; }
        private bool OnlyRowId { get; }

        private long CurrentRowId { get; set; }

        public async ValueTask DisposeAsync()
        {
            await Command.DisposeAsync().ConfigureAwait(false);
            await Reader.DisposeAsync().ConfigureAwait(false);
            await Connection.DisposeAsync().ConfigureAwait(false);
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (!await Reader.ReadAsync().ConfigureAwait(false)) return false;
            CurrentRowId = await Reader.GetFieldValueAsync<long>(0).ConfigureAwait(false);
            return true;
        }

        internal EntityEnumerator(string sql, bool onlyRowId)
        {
            OnlyRowId = onlyRowId;
            Connection = new SQLiteConnection(Settings.ConnectionString);
            Connection.Open();
            Command = new SQLiteCommand(sql, Connection);
            Reader = Command.ExecuteReader();
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
                    if (!(value is DBNull))
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