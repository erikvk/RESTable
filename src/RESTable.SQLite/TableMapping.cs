using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Resources.Templates;
using RESTable.Sqlite.Meta;

namespace RESTable.Sqlite
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a mapping between a CLR class and an Sqlite table
    /// </summary>
    [RESTable(Method.GET)]
    public class TableMapping : ISelector<TableMapping>
    {
        #region Static

        static TableMapping() => TableMappingByType = new ConcurrentDictionary<Type, TableMapping>();
        private static IDictionary<Type, TableMapping> TableMappingByType { get; }

        /// <summary>
        /// Gets the table mapping for a given CLR type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TableMapping? GetTableMapping(Type type) => TableMappingByType.SafeGet(type);

        internal static IEnumerable<TableMapping> All => TableMappingByType.Values;

        #endregion

        private Query TableInfoQuery { get; }
        private Query DropTableQuery { get; }

        #region Public

        /// <summary>
        /// The CLR class of the mapping
        /// </summary>
        [RESTableMember(ignore: true)]
        public Type CLRClass { get; }

        /// <summary>
        /// The name of the CLR class of the mapping
        /// </summary>
        public string? ClassName => CLRClass.FullName?.Replace('+', '.');

        /// <summary>
        /// The name of the mapped Sqlite table
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// The kind of this table mapping
        /// </summary>
        public TableMappingKind TableMappingKind { get; }

        /// <summary>
        /// Is this table mapping declared, as opposed to procedural?
        /// </summary>
        public bool IsDeclared { get; }

        /// <summary>
        /// The column mappings of this table mapping
        /// </summary>
        public ColumnMappings ColumnMappings { get; private set; }

        /// <summary>
        /// Does this table mapping have a corresponding Sqlite table?
        /// </summary>
        public async Task<bool> Exists() => await TableInfoQuery
            .GetRows()
            .AnyAsync()
            .ConfigureAwait(false);

        #endregion

        /// <summary>
        /// The RESTable resource instance, if any, corresponding to this mapping
        /// </summary>
        internal IEntityResource Resource { get; set; }

        internal ColumnMapping[] TransactMappings { get; private set; }

        /// <summary>
        /// The names of the mapped columns of this table mapping
        /// </summary>
        internal HashSet<string> SqlColumnNames { get; private set; }

        internal (string name, string columns, string[] param, ColumnMapping[] mappings) InsertSpec { get; private set; }
        internal (string name, string set, string[] param, ColumnMapping[] mappings) UpdateSpec { get; private set; }

        #region RESTable

        /// <inheritdoc />
        public IEnumerable<TableMapping> Select(IRequest<TableMapping> request)
        {
            return TableMappingByType.Values;
        }

        /// <inheritdoc />
        /// <summary>
        /// Options for table mappings
        /// </summary>
        [RESTable]
        public class Options : OptionsTerminal
        {
            /// <inheritdoc />
            protected override IEnumerable<Option> GetOptions()
            {
                static async ValueTask action(string[] _)
                {
                    foreach (var mapping in TableMappingByType.Values)
                        await mapping.Update().ConfigureAwait(false);
                }

                yield return new Option("Update", "Updates all table mappings", action);
            }
        }

        #endregion

        /// <summary>
        /// Creates a new table mapping, mapping a CLR class to an Sqlite table
        /// </summary>
        private TableMapping(Type clrClass)
        {
            Validate(clrClass);
            TableMappingKind = clrClass.IsSubclassOf(typeof(ElasticSqliteTable)) ? TableMappingKind.Elastic : TableMappingKind.Static;
            IsDeclared = !clrClass.Assembly.Equals(TypeBuilder.Assembly);
            CLRClass = clrClass;
            TableName = clrClass.GetCustomAttribute<SqliteAttribute>()?.CustomTableName ?? clrClass.FullName?.Replace('+', '.').Replace('.', '$')
                ?? throw new SqliteException("RESTable.SQLite encountered an unknown CLR class when creating table mappings");
            Resource = null!;
            TransactMappings = null!;
            SqlColumnNames = null!;
            TableMappingByType[CLRClass] = this;
            ColumnMappings = null!;
            TableInfoQuery = new Query($"PRAGMA table_info({TableName})");
            DropTableQuery = new Query($"DROP TABLE IF EXISTS {TableName}");
        }

        #region Helpers

        private static void Validate(Type type)
        {
            if (type.FullName is null)
                throw new SqliteException($"RESTable.SQLite encountered an unknown type: '{type.GUID}'");
            if (type.Namespace is null)
                throw new SqliteException($"RESTable.SQLite encountered a type '{type}' with no specified namespace.");
            if (type.IsGenericType)
                throw new SqliteException($"Invalid SQLite table mapping for CLR class '{type}'. Cannot map a " +
                                          "generic CLR class.");

            if (type.GetConstructor(Type.EmptyTypes) is null)
                throw new SqliteException($"Expected parameterless constructor for SQLite type '{type}'.");
            var columnProperties = type.GetDeclaredColumnProperties();
            if (columnProperties.Values.All(p => p.Name == "RowId"))
                throw new SqliteException(
                    $"No public auto-implemented instance properties found in type '{type}'. SQLite does not support empty tables, " +
                    "so each SQLiteTable must define at least one public auto-implemented instance property.");
        }

        private HashSet<string> MakeColumnNames(ColumnMappings columnMappings)
        {
            var allColumns = new HashSet<string>(columnMappings.Values.Select(c => c.SqlColumn.Name), StringComparer.OrdinalIgnoreCase);
            var notRowId = columnMappings.Values.Where(m => !m.IsRowId).ToArray();
            var columns = string.Join(", ", notRowId.Select(c => c.SqlColumn.Name));
            var mappings = notRowId;
            var param = notRowId.Select(c => $"@{c.SqlColumn.Name}").ToArray();
            var set = string.Join(", ", notRowId.Select(c => $"{c.SqlColumn.Name} = @{c.SqlColumn.Name}"));
            InsertSpec = (TableName, columns, param, mappings);
            UpdateSpec = (TableName, set, param, mappings);
            return allColumns;
        }

        internal async Task DropColumns(List<ColumnMapping> mappings)
        {
            foreach (var mapping in mappings)
                ColumnMappings.Remove(mapping.CLRProperty.Name);
            ReloadColumnNames(ColumnMappings);
            var tempColumnNames = new HashSet<string>(SqlColumnNames);
            tempColumnNames.Remove("rowid");
            var columnsSql = string.Join(", ", tempColumnNames);
            var tempName = $"__{TableName}__RESTABLE_TEMP";
            var querySql = "PRAGMA foreign_keys=off;BEGIN TRANSACTION;" +
                           $"ALTER TABLE {TableName} RENAME TO {tempName};" +
                           $"{GetCreateTableSql()}" +
                           $"INSERT INTO {TableName} ({columnsSql})" +
                           $"  SELECT {columnsSql} FROM {tempName};" +
                           $"DROP TABLE {tempName};" +
                           "COMMIT;PRAGMA foreign_keys=on;";
            var query = new Query(querySql);
            await query.ExecuteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the SQL columns of the mapped SQL table
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<SqlColumn> GetSqlColumns()
        {
            await foreach (var row in TableInfoQuery.GetRows().ConfigureAwait(false))
            {
                var name = await row.GetFieldValueAsync<string>(1).ConfigureAwait(false);
                var type = await row.GetFieldValueAsync<string>(2).ConfigureAwait(false);
                yield return new SqlColumn(name: name, type: type.ParseSqlDataType());
            }
        }

        private void ReloadColumnNames(ColumnMappings columnMappings) => SqlColumnNames = MakeColumnNames(columnMappings);

        internal async Task Update()
        {
            var columnMappings = GetDeclaredColumnMappings();
            await columnMappings.Push().ConfigureAwait(false);
            var columnNames = MakeColumnNames(columnMappings);
            await foreach (var column in GetSqlColumns().Where(column => !columnNames.Contains(column.Name)).ConfigureAwait(false))
            {
                columnMappings[column.Name] = new ColumnMapping
                (
                    tableMapping: this,
                    clrProperty: new CLRProperty(column.Name, column.Type.ToClrTypeCode()),
                    sqlColumn: column
                );
            }
            ReloadColumnNames(columnMappings);
            TransactMappings = columnMappings.Values.Where(mapping => !mapping.CLRProperty.IsIgnored).ToArray();
            ColumnMappings = columnMappings;
        }

        private async Task Drop() => await DropTableQuery.ExecuteAsync().ConfigureAwait(false);

        // ReSharper disable once ConstantNullCoalescingCondition
        private string GetCreateTableSql() => $"CREATE TABLE {TableName} ({(ColumnMappings ?? GetDeclaredColumnMappings()).ToSql()});";

        private ColumnMappings GetDeclaredColumnMappings()
        {
            var columnMappings = CLRClass
                .GetDeclaredColumnProperties()
                .Values
                .Select(property => new ColumnMapping
                (
                    tableMapping: this,
                    clrProperty: property,
                    sqlColumn: new SqlColumn(property.MemberAttribute?.ColumnName ?? property.Name, property.Type.ToSqlDataType())
                ));
            return new ColumnMappings(columnMappings);
        }

        internal static async Task CreateMapping(Type clrClass)
        {
            var mapping = new TableMapping(clrClass);
            if (!await mapping.Exists())
            {
                var createTableQuery = new Query(mapping.GetCreateTableSql());
                await createTableQuery.ExecuteAsync().ConfigureAwait(false);
            }
            await mapping.Update().ConfigureAwait(false);
        }

        internal static async Task<bool> Drop(Type? clrClass)
        {
            if (clrClass is null)
                return false;
            switch (GetTableMapping(clrClass))
            {
                case null: return false;
                case var declared when declared.IsDeclared: return false;
                case var procedural:
                {
                    await procedural.Drop().ConfigureAwait(false);
                    TableMappingByType.Remove(clrClass);
                    return true;
                }
            }
        }

        #endregion
    }
}