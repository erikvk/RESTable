using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Operations;

namespace RESTar.SQLite
{
    internal static class SQLiteConfig
    {
        private static string SQLiteDb { get; set; }
        private static string ConnectionString { get; set; }

        internal static void Init(SQLiteAddOnInfo addOnInfo)
        {
            if (!Regex.IsMatch(addOnInfo.DatabaseName, @"^[a-zA-Z0-9_]+$"))
                throw new SQLiteException($"SQLite database name '{addOnInfo.DatabaseName}' contains invalid characters: " +
                                          "Only letters, numbers and underscores are valid in SQLite database names.");
            SetupDatabase(addOnInfo);
            SetupResources();
        }

        private static void SetupDatabase(SQLiteAddOnInfo addOnInfo)
        {
            var databasePath = $"{addOnInfo.DatabaseDirectory}\\{addOnInfo.DatabaseName}.sqlite";
            if (!Directory.Exists(addOnInfo.DatabaseDirectory))
                Directory.CreateDirectory(addOnInfo.DatabaseDirectory);
            if (!File.Exists(databasePath))
                SQLiteConnection.CreateFile(databasePath);
            SQLiteDb = databasePath;
            ConnectionString = $"Data Source={databasePath};Version=3;";
        }

        private static void SetupResources()
        {
            typeof(object).GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>() && t.HasAttribute<SQLiteAttribute>())
                .ForEach(t =>
                {
                    Do.TryCatch(() => Resource.AutoRegister(t), e => throw (e.InnerException ?? e));
                });


        }
    }
}