using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar.Admin
{
    /// <summary>
    /// The DatabaseIndex resource lets an administrator set indexes for Starcounter database resources.
    /// </summary>
    [RESTar(Description = description)]
    public class DatabaseIndex : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex>, IValidatable
    {
        private const string description = "The DatabaseIndex resource lets an administrator set " +
                                           "indexes for Starcounter database resources.";

        /// <summary>
        /// The name of the index
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The table for which this index is registered
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// The columns on which this index is registered
        /// </summary>
        public ColumnInfo[] Columns { get; set; }

        /// <summary>
        /// Creates an ascending database index for the table T with a given name on the given column.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string name, string columnName)
            where T : class => Register<T>(name, (columnName, false));

        /// <summary>
        /// Creates an ascending database index for the table T with a given name on the given columns.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string name, string columnName1, string columnName2)
            where T : class => Register<T>(name, (columnName1, false), (columnName2, false));

        /// <summary>
        /// Creates a database index for a table type with a given name on the given column and direction.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string name, string columnName, bool descending)
            where T : class => Register<T>(name, (columnName, descending));

        /// <summary>
        /// Creates a database index for a table type with a given name on a given list of columns.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string name, params ColumnInfo[] columns) where T : class
        {
            SelectionCondition.Value = name;
            SelectionRequest.PUT(() => new DatabaseIndex
            {
                Table = typeof(T).FullName,
                Name = name,
                Columns = columns
            });
        }

        private static readonly Condition<DatabaseIndex> SelectionCondition;
        private static readonly Request<DatabaseIndex> SelectionRequest;

        static DatabaseIndex()
        {
            SelectionCondition = new Condition<DatabaseIndex>(nameof(Name), Operator.EQUALS, null);
            SelectionRequest = new Request<DatabaseIndex>(SelectionCondition);
        }

        private const string ColumnSql = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                         "WHERE t.\"Index\" =? ORDER BY t.Position";

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request) => Db
            .SQL<Index>("SELECT t FROM Starcounter.Metadata.\"Index\" t")
            .Where(index => !index.Table.FullName.StartsWith("Starcounter."))
            .Where(index => !index.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
            .Select(index => new DatabaseIndex
            {
                Name = index.Name,
                Table = index.Table.FullName,
                Columns = Db.SQL<IndexedColumn>(ColumnSql, index).Select(c => new ColumnInfo
                {
                    Name = c.Column.Name,
                    Descending = c.Ascending == 0
                }).ToArray()
            })
            .Where(request.Conditions);

        /// <inheritdoc />
        public int Insert(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Table} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Table}");
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Table} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Table}");
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public bool IsValid(out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(Table))
            {
                invalidReason = "Index table cannot be null or whitespace";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                invalidReason = "Index name cannot be null or whitespace";
                return false;
            }
            if (Columns?.Any() != true)
            {
                invalidReason = "No columns specified for index";
                return false;
            }
            invalidReason = null;
            return true;
        }
    }

    /// <summary>
    /// Contains information about a column on which an index is registered.
    /// </summary>
    public class ColumnInfo
    {
        /// <summary>
        /// The name of the column (property name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Is this index descending? (otherwise ascending)
        /// </summary>
        public bool Descending { get; set; }

        /// <summary>
        /// Creates a new ColumnInfo from a tuple describing a column name and direction
        /// </summary>
        /// <param name="column"></param>
        public static implicit operator ColumnInfo((string Name, bool Descending) column) => new ColumnInfo
        {
            Name = column.Name,
            Descending = column.Descending
        };
    }
}