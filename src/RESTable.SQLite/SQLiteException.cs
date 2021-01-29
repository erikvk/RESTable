using System;

namespace RESTable.SQLite
{
    /// <inheritdoc />
    public class SQLiteException : Exception
    {
        internal SQLiteException(string message) : base(message) { }
    }
}