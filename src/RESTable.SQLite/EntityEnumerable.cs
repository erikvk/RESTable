using System.Collections.Generic;
using System.Threading;

namespace RESTable.SQLite
{
    internal class EntityEnumerable<T> : IAsyncEnumerable<T> where T : SQLiteTable
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => Enumerator;
        private IAsyncEnumerator<T> Enumerator { get; }
        internal EntityEnumerable(string sql, bool onlyRowId) => Enumerator = new EntityEnumerator<T>(sql, onlyRowId);
    }
}