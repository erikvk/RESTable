using System.Collections.Generic;
using System.Threading;

namespace RESTable.SQLite
{
    internal class EntityEnumerable<T> : IAsyncEnumerable<T> where T : SQLiteTable
    {
        internal string Sql { get; }
        internal bool OnlyRowId { get; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            return new EntityEnumerator<T>(Sql, OnlyRowId, cancellationToken);
        }

        internal EntityEnumerable(string sql, bool onlyRowId)
        {
            Sql = sql;
            OnlyRowId = onlyRowId;
        }
    }
}