using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using Starcounter;
using IResource = RESTar.Internal.IResource;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.SQLite
{
    public class SQLiteProvider : ResourceProvider<SQLiteTable>
    {
        public override bool IsValid(Type type, out string reason)
        {
            var columnProperties = type.GetProperties()
                .Where(p => p.GetCustomAttribute<ColumnAttribute>() != null)
                .ToList();

            if (!typeof(SQLiteTable).IsAssignableFrom(type))
            {
                reason = $"Resource type '{type.FullName}' does not subclass the '{typeof(SQLiteTable).FullName}' " +
                         "abstract class needed for all SQLite resource types.";
                return false;
            }

            foreach (var column in columnProperties)
            {
                if (!column.PropertyType.IsSQLiteCompatibleValueType(type, out var error))
                {
                    reason = error;
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public SQLiteProvider(string databaseDirectory, string databaseName)
        {
            if (!Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
                throw new SQLiteException($"SQLite database name '{databaseName}' contains invalid characters: " +
                                          "Only letters, numbers and underscores are valid in SQLite database names.");
            var databasePath = $"{databaseDirectory}\\{databaseName}.sqlite";
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);
            if (!File.Exists(databasePath))
                SQLiteConnection.CreateFile(databasePath);
            Db.TransactAsync(() =>
            {
                Settings.All.ForEach(Db.Delete);
                new Settings
                {
                    DatabasePath = databasePath,
                    DatabaseDirectory = databaseDirectory,
                    DatabaseName = databaseName,
                    DatabaseConnectionString = $"Data Source={databasePath};Version=3;"
                };
            });
            DatabaseIndexer = new SQLiteIndexer();
        }

        public override void ReceiveClaimed(ICollection<IResource> claimedResources)
        {
            SQLiteDb.SetupTables(claimedResources);
        }

        public override Type AttributeType => typeof(SQLiteAttribute);
        public override Selector<T> GetDefaultSelector<T>() => SQLiteOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => SQLiteOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => SQLiteOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => SQLiteOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => SQLiteOperations<T>.Count;
        public override Profiler GetProfiler<T>() => null;
    }
}