using System;

namespace RESTable.Sqlite;

/// <inheritdoc />
public class SqliteException : Exception
{
    internal SqliteException(string message) : base(message) { }
}
