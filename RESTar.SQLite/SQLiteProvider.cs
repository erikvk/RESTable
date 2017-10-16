using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using Starcounter;
using IResource = RESTar.Internal.IResource;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.SQLite
{
    public class SQLiteProvider : ResourceProvider<object>
    {
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
        }

        public override void ReceiveClaimed(ICollection<IResource> claimedResources)
        {
            SQLiteDb.SetupTables(claimedResources);
        }

        public override Type AttributeType => typeof(SQLiteAttribute);

        public override Selector<T> GetDefaultSelector<T>() => request =>
        {
            var (dbConditions, postConditions) = request.Conditions.Split(c =>
                c.Term.Count == 1 &&
                c.Term.First is StaticProperty stat &&
                stat.HasAttribute<ColumnAttribute>()
            );

            var rawQuery = $"SELECT * FROM {request.Resource.GetSQLiteTableName()} " +
                           dbConditions.ToSQLiteWhereClause();

            var columns = request.Resource.GetColumns();
            var results = new List<T>();

            SQLiteDb.Select(rawQuery, row =>
            {
                var t = Activator.CreateInstance<T>();
                for (var i = 0; i < columns.Length; i += 1)
                    columns[i].SetValue(t, row.GetValue(i));
                results.Add(t);
            });

            return results.Where(postConditions);
        };

        public override Inserter<T> GetDefaultInserter<T>() => (entities, request) =>
        {
            var columns = request.Resource.GetColumns();
            var count = 0;
            var sqlStub = $"INSERT INTO {request.Resource.GetSQLiteTableName()} VALUES (";
            foreach (var entity in entities)
                SQLiteDb.Query($"{sqlStub}{entity.ToSQLiteInsertInto(columns)})", c => count += c.ExecuteNonQuery());
            return count;
        };

        public override Updater<T> GetDefaultUpdater<T>() => null;

        public override Deleter<T> GetDefaultDeleter<T>() => null;

        public override Counter<T> GetDefaultCounter<T>() => null;
        public override Profiler GetProfiler<T>() => null;
    }
}