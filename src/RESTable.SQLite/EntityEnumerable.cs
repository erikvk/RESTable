using System.Collections;
using System.Collections.Generic;

namespace RESTable.SQLite
{
    internal class EntityEnumerable<T> : IEnumerable<T> where T : SQLiteTable
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private EntityEnumerator<T> Enumerator { get; }
        internal EntityEnumerable(string sql, bool onlyRowId) => Enumerator = new EntityEnumerator<T>(sql, onlyRowId);
        public IEnumerator<T> GetEnumerator() => Enumerator;
    }
}