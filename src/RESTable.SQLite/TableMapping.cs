using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Admin;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Resources.Templates;
using RESTable.SQLite.Meta;

namespace RESTable.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a mapping between a CLR class and an SQLite table
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
        public static TableMapping GetTableMapping(Type type) => TableMappingByType.SafeGet(type);

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
        public string ClassName => CLRClass.FullName?.Replace('+', '.');

        /// <summary>
        /// The name of the mapped SQLite table
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
        /// Does this table mapping have a corresponding SQL table?
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

        internal IEnumerable<ColumnMapping> TransactMappings { get; private set; }

        /// <summary>
        /// The names of the mapped columns of this table mapping
        /// </summary>
        internal HashSet<string> SQLColumnNames { get; private set; }

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
                async ValueTask Action(string[] _)
                {
                    foreach (var mapping in TableMappingByType.Values)
                        await mapping.Update().ConfigureAwait(false);
                }

                yield return new Option("Update", "Updates all table mappings", Action);
            }
        }

        #endregion

        /// <summary>
        /// Creates a new table mapping, mapping a CLR class to an SQL table
        /// </summary>
        private TableMapping(Type clrClass)
        {
            Validate(clrClass);
            TableMappingKind = clrClass.IsSubclassOf(typeof(ElasticSQLiteTable)) ? TableMappingKind.Elastic : TableMappingKind.Static;
            IsDeclared = !clrClass.Assembly.Equals(TypeBuilder.Assembly);
            CLRClass = clrClass;
            TableName = clrClass.GetCustomAttribute<SQLiteAttribute>()?.CustomTableName ?? clrClass.FullName?.Replace('+', '.').Replace('.', '$')
                ?? throw new SQLiteException("RESTable.SQLite encountered an unknown CLR class when creating table mappings");
            TableMappingByType[CLRClass] = this;
            TableInfoQuery = new Query($"PRAGMA table_info({TableName})");
            DropTableQuery = new Query($"DROP TABLE IF EXISTS {TableName}");
        }

        #region Helpers

        private static void Validate(Type type)
        {
            if (type.FullName is null)
                throw new SQLiteException($"RESTable.SQLite encountered an unknown type: '{type.GUID}'");
            if (type.Namespace is null)
                throw new SQLiteException($"RESTable.SQLite encountered a type '{type}' with no specified namespace.");
            if (type.IsGenericType)
                throw new SQLiteException($"Invalid SQLite table mapping for CLR class '{type}'. Cannot map a " +
                                          "generic CLR class.");

            if (type.GetConstructor(Type.EmptyTypes) is null)
                throw new SQLiteException($"Expected parameterless constructor for SQLite type '{type}'.");
            var columnProperties = type.GetDeclaredColumnProperties();
            if (columnProperties.Values.All(p => p.Name == "RowId"))
                throw new SQLiteException(
                    $"No public auto-implemented instance properties found in type '{type}'. SQLite does not support empty tables, " +
                    "so each SQLiteTable must define at least one public auto-implemented instance property.");
        }

        private HashSet<string> MakeColumnNames()
        {
            var allColumns = new HashSet<string>(ColumnMappings.Values.Select(c => c.SQLColumn.Name), StringComparer.OrdinalIgnoreCase);
            var notRowId = ColumnMappings.Values.Where(m => !m.IsRowId).ToArray();
            var columns = string.Join(", ", notRowId.Select(c => c.SQLColumn.Name));
            var mappings = notRowId;
            var param = notRowId.Select(c => $"@{c.SQLColumn.Name}").ToArray();
            var set = string.Join(", ", notRowId.Select(c => $"{c.SQLColumn.Name} = @{c.SQLColumn.Name}"));
            InsertSpec = (TableName, columns, param, mappings);
            UpdateSpec = (TableName, set, param, mappings);
            return allColumns;
        }

        internal async Task DropColumns(IRequest request, List<ColumnMapping> mappings)
        {
            foreach (var mapping in mappings)
                ColumnMappings.Remove(mapping.CLRProperty.Name);
            ReloadColumnNames();
            var tempColumnNames = new HashSet<string>(SQLColumnNames);
            tempColumnNames.Remove("rowid");
            var columnsSQL = string.Join(", ", tempColumnNames);
            var tempName = $"__{TableName}__RESTABLE_TEMP";
            var querySql = "PRAGMA foreign_keys=off;BEGIN TRANSACTION;" +
                           $"ALTER TABLE {TableName} RENAME TO {tempName};" +
                           $"{GetCreateTableSql()}" +
                           $"INSERT INTO {TableName} ({columnsSQL})" +
                           $"  SELECT {columnsSQL} FROM {tempName};" +
                           $"DROP TABLE {tempName};" +
                           "COMMIT;PRAGMA foreign_keys=on;";
            var query = new Query(querySql);
            await using var indexRequest = request
                .GetRequiredService<RootContext>()
                .CreateRequest<DatabaseIndex>()
                .WithCondition(
                    key: nameof(DatabaseIndex.ResourceName),
                    op: Operators.EQUALS,
                    value: Resource.Name
                );
            var entities = indexRequest.GetResultEntities();
            var tableIndexesToKeep = await entities
                .Where(index => !index.Columns.Any(column => mappings.Any(mapping => column.Name.EqualsNoCase(mapping.SQLColumn.Name))))
                .ToListAsync()
                .ConfigureAwait(false);
            await query.Execute().ConfigureAwait(false);
            indexRequest.Method = Method.POST;
            indexRequest.Selector = () => tableIndexesToKeep.ToAsyncEnumerable();
            var result = await indexRequest.GetResult().ConfigureAwait(false);
            await using (result.ConfigureAwait(false))
            {
                result.ThrowIfError();
                await Update().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the SQL columns of the mapped SQL table
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<SQLColumn> GetSqlColumns()
        {
            await foreach (var row in TableInfoQuery.GetRows().ConfigureAwait(false))
            {
                var name = await row.GetFieldValueAsync<string>(1).ConfigureAwait(false);
                var type = await row.GetFieldValueAsync<string>(2).ConfigureAwait(false);
                yield return new SQLColumn(name: name, type: type.ParseSQLDataType());
            }
        }

        private void ReloadColumnNames() => SQLColumnNames = MakeColumnNames();

        internal async Task Update()
        {
            ColumnMappings = GetDeclaredColumnMappings();
            await ColumnMappings.Push().ConfigureAwait(false);
            var columnNames = MakeColumnNames();
            await foreach (var column in GetSqlColumns().Where(column => !columnNames.Contains(column.Name)).ConfigureAwait(false))
            {
                ColumnMappings[column.Name] = new ColumnMapping
                (
                    tableMapping: this,
                    clrProperty: new CLRProperty(column.Name, column.Type.ToCLRTypeCode()),
                    sqlColumn: column
                );
            }
            ReloadColumnNames();
            TransactMappings = ColumnMappings.Values.Where(mapping => !mapping.CLRProperty.IsIgnored).ToArray();
        }

        private async Task Drop() => await DropTableQuery.Execute().ConfigureAwait(false);

        private string GetCreateTableSql() => $"CREATE TABLE {TableName} ({(ColumnMappings ?? GetDeclaredColumnMappings()).ToSQL()});";

        private ColumnMappings GetDeclaredColumnMappings() => CLRClass
            .GetDeclaredColumnProperties()
            .Values
            .Select(property => new ColumnMapping
            (
                tableMapping: this,
                clrProperty: property,
                sqlColumn: new SQLColumn(property.MemberAttribute?.ColumnName ?? property.Name, property.Type.ToSQLDataType())
            ))
            .ToColumnMappings();

        internal static async Task CreateMapping(Type clrClass)
        {
            var mapping = new TableMapping(clrClass);
            if (!await mapping.Exists())
            {
                var createTableQuery = new Query(mapping.GetCreateTableSql());
                await createTableQuery.Execute().ConfigureAwait(false);
            }
            await mapping.Update().ConfigureAwait(false);
        }

        internal static async Task<bool> Drop(Type clrClass)
        {
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