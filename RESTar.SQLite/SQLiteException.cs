using System;

namespace RESTar.SQLite
{
    public class SQLiteException : Exception
    {
        public SQLiteException(string message) : base(message)
        {
        }
    }
}