using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Internal;

namespace RESTar.Admin
{
    /// <summary>
    /// The DatabaseIndex resource lets an administrator set indexes for RESTar database resources.
    /// </summary>
    [RESTar(Description = description)]
    public class DatabaseIndex : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex>, IValidatable
    {
        private const string description = "The DatabaseIndex resource lets an administrator set " +
                                           "indexes for Starcounter database resources.";

        private string _name;

        /// <summary>
        /// The name of the index
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Name));
                if (!Regex.IsMatch(value, RegEx.LettersNumsAndUs))
                    throw new FormatException("Index name contained invalid characters. Can only contain " +
                                              "letters, numbers and underscores");

                _name = value;
            }
        }

        private string _table;

        /// <summary>
        /// The name of the RESTar resource corresponding with the database table on which 
        /// this index is registered
        /// </summary>
        public string Table
        {
            get => _table;
            set
            {
                IResource = RESTar.Resource.GetEntityResource(value);
                _table = IResource.Name;
                Provider = IResource.Provider;
            }
        }

        /// <summary>
        /// The resource provider that generated this index
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// The RESTar resource corresponding to the table on which this index is registered
        /// </summary>
        [IgnoreDataMember] public IEntityResource IResource { get; private set; }

        /// <summary>
        /// The columns on which this index is registered
        /// </summary>
        public ColumnInfo[] Columns { get; set; }

        /// <inheritdoc />
        [JsonConstructor]
        public DatabaseIndex(string table)
        {
            if (string.IsNullOrWhiteSpace(table))
                throw new Exception("Found no resource to register index on. Resource was null or empty");
            Table = table;
        }

        #region Public helpers

        /// <summary>
        /// Creates an ascending database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string indexName, params string[] columnNames) where T : class
        {
            Register<T>(indexName, columnNames.Select(columnName => (ColumnInfo) (columnName, false)).ToArray());
        }

        /// <summary>
        /// Creates a database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string indexName, params (string columnName, bool descending)[] columns) where T : class
        {
            Register<T>(indexName, columns.Select(column => (ColumnInfo) column).ToArray());
        }

        /// <summary>
        /// Creates a database index for a table type with a given name on a given list of columns.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        private static void Register<T>(string indexName, params ColumnInfo[] columns) where T : class
        {
            SelectionCondition.Value = indexName;
            SelectionRequest.PUT(() => new DatabaseIndex(typeof(T).FullName)
            {
                Name = indexName,
                Columns = columns
            });
        }

        #endregion

        private static Condition<DatabaseIndex> SelectionCondition { get; set; }
        private static Request<DatabaseIndex> SelectionRequest { get; set; }
        internal static readonly Dictionary<string, IDatabaseIndexer> Indexers;
        static DatabaseIndex() => Indexers = new Dictionary<string, IDatabaseIndexer>();

        internal static void Init()
        {
            SelectionCondition = new Condition<DatabaseIndex>(nameof(Name), Operators.EQUALS, null);
            SelectionRequest = new Request<DatabaseIndex>(SelectionCondition);
        }

        /// <inheritdoc />
        public bool IsValid(out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(Table))
            {
                invalidReason = "Index resource name cannot be null or whitespace";
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

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request) => Indexers
            .Values
            .Distinct()
            .SelectMany(indexer => indexer.Select(request));

        /// <inheritdoc />
        public int Insert(IRequest<DatabaseIndex> request) => request.GetEntities()
            .GroupBy(index => index.IResource.Provider)
            .Sum(group =>
            {
                var requestinternal = (IRequestInternal<DatabaseIndex>) request;
                if (!Indexers.TryGetValue(group.Key, out var indexer))
                    throw new Exception($"Unable to register index. Resource '{group.First().IResource.Name}' " +
                                        "is not a database resource.");
                requestinternal.EntitiesGenerator = () => group;
                return indexer.Insert(requestinternal);
            });

        /// <inheritdoc />
        public int Update(IRequest<DatabaseIndex> request) => request.GetEntities()
            .GroupBy(index => index.IResource.Provider)
            .Sum(group =>
            {
                var requestinternal = (IRequestInternal<DatabaseIndex>) request;
                requestinternal.EntitiesGenerator = () => group;
                return Indexers[group.Key].Update(requestinternal);
            });

        /// <inheritdoc />
        public int Delete(IRequest<DatabaseIndex> request) => request.GetEntities()
            .GroupBy(index => index.IResource.Provider)
            .Sum(group =>
            {
                var requestinternal = (IRequestInternal<DatabaseIndex>) request;
                requestinternal.EntitiesGenerator = () => group;
                return Indexers[group.Key].Delete(requestinternal);
            });
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