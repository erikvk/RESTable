﻿using System;
using System.Data.SQLite;

namespace RESTable.SQLite
{
    internal static class Database
    {
        internal static int Query(string sql)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return command.ExecuteNonQuery();
            }
        }

        internal static void Query(string sql, Action<SQLiteDataReader> rowAction)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn())
            using (var command = new SQLiteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    rowAction(reader);
        }

        internal static int Transact(Func<SQLiteCommand, int> callback)
        {
            return Transact((connection, command) => callback(command));
        }

        internal static int Transact(Func<SQLiteConnection, SQLiteCommand, int> callback)
        {
            using (var connection = new SQLiteConnection(Settings.ConnectionString).OpenAndReturn())
            using (var command = connection.CreateCommand())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var result = callback(connection, command);
                    transaction.Commit();
                    return result;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}