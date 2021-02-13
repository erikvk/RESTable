﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Internal.EntityResourceProviderController;

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncInserter{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <inheritdoc cref="IAsyncValidator{T}" />
    /// <summary>
    /// The DatabaseIndex resource lets an administrator set indexes for RESTable database resources.
    /// </summary>
    [RESTable(Description = description)]
    public class DatabaseIndex : IAsyncSelector<DatabaseIndex>, IAsyncInserter<DatabaseIndex>, IAsyncUpdater<DatabaseIndex>,
        IAsyncDeleter<DatabaseIndex>, IValidator<DatabaseIndex>
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

        private string _resourceName;

        /// <summary>
        /// The full name of the RESTable resource corresponding with the database table on which 
        /// this index is registered
        /// </summary>
        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (!Meta.Resource.TryFind<IEntityResource>(value, out var resource, out var error))
                    throw error;
                Resource = resource;
                _resourceName = Resource.Name;
                Provider = Resource.Provider;
            }
        }

        /// <summary>
        /// The resource provider that generated this index
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// The RESTable resource corresponding to the table on which this index is registered
        /// </summary>
        [RESTableMember(ignore: true)]
        public IEntityResource Resource { get; private set; }

        /// <summary>
        /// The columns on which this index is registered
        /// </summary>
        public ColumnInfo[] Columns { get; set; }

        [JsonConstructor]
        public DatabaseIndex(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new Exception("Found no resource to register the index on. The 'ResourceName' " +
                                    "property was null or empty");
            ResourceName = resourceName;
        }

        #region Public helpers

        /// <summary>
        /// Creates an ascending database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static async Task Register<T>(string indexName, params string[] columnNames) where T : class
        {
            await Register<T>(indexName, columnNames.Select(columnName => (ColumnInfo) (columnName, false)).ToArray());
        }

        /// <summary>
        /// Creates a database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static async Task Register<T>(string indexName, params (string columnName, bool descending)[] columns) where T : class
        {
            await Register<T>(indexName, columns.Select(column => (ColumnInfo) column).ToArray());
        }

        /// <summary>
        /// Creates a database index for a table type with a given name on a given list of columns.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        private static async Task Register<T>(string indexName, params ColumnInfo[] columns) where T : class
        {
            if (!RESTableConfig.Initialized)
                throw new NotInitializedException(
                    $"Invalid call to DatabaseIndex.Register() with index name '{indexName}' for type '{typeof(T)}'. " +
                    "Cannot register database indexes before RESTableConfig.Init() has been called.");
            SelectionCondition.Value = indexName;

            SelectionRequest.Selector = () => new DatabaseIndex(typeof(T).GetRESTableTypeName()) {Name = indexName, Columns = columns}.ToAsyncSingleton();
            var result = await SelectionRequest.Evaluate();
            result.ThrowIfError();
        }

        #endregion

        private static Condition<DatabaseIndex> SelectionCondition { get; set; }

        private static IRequest<DatabaseIndex> SelectionRequest { get; set; }
        //internal static readonly Dictionary<string, IDatabaseIndexer> Indexers;
        //static DatabaseIndex() => Indexers = new Dictionary<string, IDatabaseIndexer>();

        internal static void Init()
        {
            SelectionCondition = new Condition<DatabaseIndex>(nameof(Name), Operators.EQUALS, null);
            SelectionRequest = RESTableContext.Root.CreateRequest<DatabaseIndex>(Method.PUT);
            SelectionRequest.Conditions.Add(SelectionCondition);
        }

        /// <inheritdoc />
        public bool IsValid(DatabaseIndex entity, out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(entity.ResourceName))
            {
                invalidReason = "Index resource name cannot be null or whitespace";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                invalidReason = "Index name cannot be null or whitespace";
                return false;
            }
            if (entity.Columns?.Any() != true)
            {
                invalidReason = "No columns specified for index";
                return false;
            }

            invalidReason = null;
            return true;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DatabaseIndex> SelectAsync(IRequest<DatabaseIndex> request)
        {
            foreach (var indexer in EntityResourceProviders
                .Values
                .Select(p => p.DatabaseIndexer)
                .Where(indexer => indexer != null)
                .Distinct())
            await foreach (var index in indexer.SelectAsync(request))
                yield return index;
        }

        /// <inheritdoc />
        public async Task<int> InsertAsync(IRequest<DatabaseIndex> request)
        {
            var count = 0;
            var entities = request.GetInputEntitiesAsync();
            if (entities == null) return 0;
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider))
            {
                if (!EntityResourceProviders.TryGetValue(@group.Key, out var provider) || !(provider.DatabaseIndexer is IDatabaseIndexer indexer))
                    throw new Exception($"Unable to register index. Resource '{(await group.FirstAsync()).Resource.Name}' is not a database resource.");
                request.Selector = () => group;
                count += await indexer.InsertAsync(request);
            }
            return count;
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(IRequest<DatabaseIndex> request)
        {
            var count = 0;
            var entities = request.GetInputEntitiesAsync();
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider))
            {
                request.Updater = _ => group;
                count += await EntityResourceProviders[@group.Key].DatabaseIndexer.UpdateAsync(request);
            }
            return count;
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IRequest<DatabaseIndex> request)
        {
            var count = 0;
            var entities = request.GetInputEntitiesAsync();
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider))
            {
                request.Selector = () => group;
                count += await EntityResourceProviders[@group.Key].DatabaseIndexer.DeleteAsync(request);
            }
            return count;
        }
    }
}