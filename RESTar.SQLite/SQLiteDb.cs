using System;
using System.Collections.Generic;
using System.Data.SQLite;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.SQLite
{
    internal static class SQLiteDb
    {
        internal static void SetupTables(ICollection<IResource> resources) => resources.ForEach(r =>
        {
            var createTableSQL = r.MakeCreateTableQuery();
            Query(createTableSQL, command => command.ExecuteNonQuery());
        });

        internal static void Query(string sql, Action<SQLiteCommand> action)
        {
            using (var connection = new SQLiteConnection(Settings.Instance.DatabaseConnectionString))
            {
                connection.Open();
                action(new SQLiteCommand(sql, connection));
            }
        }

        internal static void Select(string sql, Action<SQLiteDataReader> rowAction)
        {
            using (var connection = new SQLiteConnection(Settings.Instance.DatabaseConnectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(sql, connection);
                using (var reader = command.ExecuteReader())
                    while (reader.Read()) rowAction(reader);
            }
        }
    }
}