using System;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Internal;
using System.Linq;
using Starcounter;
using Starcounter.Metadata;
using static RESTar.RESTarPresets;

namespace RESTar
{
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

    /// <summary>
    /// A resource for handling database indexes for Starcounter resources.
    /// </summary>
    [RESTar(ReadAndWrite)]
    public class DatabaseIndex : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex>
    {
        /// <summary>
        /// The name of the index
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The resource (table) for which this index is registered
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// The columns on which this index is registered
        /// </summary>
        public ColumnInfo[] Columns { get; set; }

        private const string IndexSQL = "SELECT t FROM Starcounter.Metadata.\"Index\" t";

        private const string ColumnSQL = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                         "WHERE t.\"Index\" =? ORDER BY t.Position";

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
            var resource = Resource<T>.Get;
            if (resource == null)
                throw new UnknownResourceException(typeof(T).FullName);
            if (resource.ResourceType != RESTarResourceType.StaticStarcounter)
                throw new Exception("Database indexes can only be registered for static Starcounter resources." +
                                    $"Resource '{resource.AliasOrName}' is of type '{resource.ResourceType}'.");
            InternalRequest.Conditions[0].SetValue(name);
            InternalRequest.PUT(() => new DatabaseIndex
            {
                Resource = typeof(T).FullName,
                Name = name,
                Columns = columns
            });
        }

        private static readonly Request<DatabaseIndex> InternalRequest = new Request<DatabaseIndex>("Name", "=", null);

        private static IEnumerable<DatabaseIndex> All => Db
            .SQL<Index>(IndexSQL)
            .Where(i => !i.Table.FullName.StartsWith("Starcounter."))
            .Where(i => !i.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
            .Select(i =>
            {
                var columns = Db.SQL<IndexedColumn>(ColumnSQL, i);
                return new DatabaseIndex
                {
                    Name = i.Name,
                    Resource = i.Table.FullName,
                    Columns = columns.Select(c => new ColumnInfo
                    {
                        Name = c.Column.Name,
                        Descending = c.Ascending == 0
                    }).ToArray()
                };
            })
            .ToList();

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request) => All
            .Where(request.Conditions)
            .ToList();

        /// <summary>
        /// </summary>
        public int Insert(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Resource} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// </summary>
        public int Update(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Resource}");
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Resource} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// </summary>
        public int Delete(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Resource}");
                count += 1;
            }
            return count;
        }
    }
}