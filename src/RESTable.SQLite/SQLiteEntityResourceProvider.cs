﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.SQLite
{
    /// <inheritdoc cref="EntityResourceProvider{TBase}" />
    /// <inheritdoc cref="IProceduralEntityResourceProvider" />
    /// <summary>
    /// A resource provider for the SQLite database system. To use, include an instance of this class 
    /// in the call to RESTableConfig.Init(). To register SQLite resources, create subclasses of SQLiteTable
    /// and decorate them with the SQLiteAttribute together with the RESTableAttribute. Public instance 
    /// properties can be mapped to columns in SQLite by decorating the with the ColumnAttribute. All O/RM 
    /// mapping and query building is done by RESTable. Use the DatabaseIndex resource to register indexes 
    /// for SQLite resources (just like you would for Starcounter resources).
    /// </summary>
    public class SQLiteEntityResourceProvider : EntityResourceProvider<SQLiteTable>, IProceduralEntityResourceProvider
    {
        private static bool IsInitiated { get; set; }

        private static void Init()
        {
            if (IsInitiated) return;
            foreach (var clrClass in typeof(SQLiteTable).GetConcreteSubclasses())
                TableMapping.CreateMapping(clrClass).Wait();
            IsInitiated = true;
        }

        /// <inheritdoc />
        protected override bool IsValid(IEntityResource resource, TypeCache typeCache, out string reason)
        {
            reason = null;
            return true;
        }

        /// <inheritdoc />
        protected override void ModifyResourceAttribute(Type type, RESTableAttribute attribute)
        {
            if (type.IsSubclassOf(typeof(ElasticSQLiteTable)))
            {
                attribute.AllowDynamicConditions = true;
                attribute.FlagStaticMembers = true;
            }
        }

        /// <inheritdoc />
        protected override IDatabaseIndexer DatabaseIndexer { get; }

        /// <summary>
        /// Creates a new instance of the SQLiteProvider class, for use in calls to RESTableConfig.Init()
        /// </summary>
        /// <param name="databasePath">A path to the SQLite database file to use with RESTable.SQLite. If no
        /// such file exists, one will be created.</param>
        public SQLiteEntityResourceProvider(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new SQLiteException("The SQLite database path cannot be null, empty or whitespace");
            var (directory, fileName, extension) = (Path.GetDirectoryName(databasePath), Path.GetFileNameWithoutExtension(databasePath),
                Path.GetExtension(databasePath));
            if (string.IsNullOrWhiteSpace(extension))
            {
                if (Directory.Exists(databasePath))
                    throw new SQLiteException(
                        $"The SQLite database path '{databasePath}' was invalid. Must be an absolute file path, " +
                        "including file name. Found reference to folder.");
                databasePath += ".sqlite";
            }

            if (string.IsNullOrWhiteSpace(directory))
                throw new SQLiteException(
                    $"The SQLite database path '{databasePath}' was invalid. Must be an absolute file path, including file name.");
            if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9_]+$"))
                throw new SQLiteException($"SQLite database file name '{fileName}' contains invalid characters: " +
                                          "Only letters, numbers and underscores are valid in SQLite database file names.");

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (!File.Exists(databasePath)) SQLiteConnection.CreateFile(databasePath);

            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databasePath,
                Version = 3,
                DateTimeKind = DateTimeKind.Utc
            };

            Settings.Instance = new Settings
            {
                DatabasePath = databasePath,
                DatabaseDirectory = directory,
                DatabaseName = fileName,
                DatabaseConnectionString = builder.ToString()
            };
            DatabaseIndexer = new SQLiteIndexer();
            Init();
        }

        /// <inheritdoc />
        protected override void ReceiveClaimed(ICollection<IEntityResource> claimedResources)
        {
            foreach (var claimed in claimedResources)
            {
                var tableMapping = TableMapping.GetTableMapping(claimed.Type) ?? throw new SQLiteException(
                    $"A resource '{claimed}' was claimed by the SQLite resource provider, " +
                    "but had no existing table mapping");
                tableMapping.Resource = claimed;
            }
        }

        /// <inheritdoc />
        protected override Type AttributeType => typeof(SQLiteAttribute);

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultSelectAsync<T>(IRequest<T> request) => SQLiteOperations<T>.SelectAsync(request);

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultInsertAsync<T>(IRequest<T> request) => SQLiteOperations<T>.InsertAsync(request);

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultUpdateAsync<T>(IRequest<T> request) => SQLiteOperations<T>.UpdateAsync(request);

        /// <inheritdoc />
        protected override ValueTask<int> DefaultDeleteAsync<T>(IRequest<T> request) => SQLiteOperations<T>.DeleteAsync(request);

        /// <inheritdoc />
        protected override ValueTask<long> DefaultCountAsync<T>(IRequest<T> request) => SQLiteOperations<T>.CountAsync(request);

        /// <inheritdoc />
        protected override IEnumerable<IProceduralEntityResource> SelectProceduralResources()
        {
            foreach (var resource in SQLite<ProceduralResource>.Select().ToEnumerable())
            {
                var type = resource.Type;
                if (type != null)
                {
                    if (TableMapping.GetTableMapping(type) is null)
                        TableMapping.CreateMapping(type).Wait();
                    yield return resource;
                }
            }
        }

        /// <inheritdoc />
        protected override IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data)
        {
            var resource = new ProceduralResource
            {
                Name = name,
                Description = description,
                Methods = methods,
                BaseTypeName = data.BaseTypeName ?? throw new SQLiteException("No BaseTypeName defined in 'Data' in resource controller")
            };
            var resourceType = resource.Type;
            if (resourceType is null)
                throw new SQLiteException(
                    $"Could not locate basetype '{resource.BaseTypeName}' when building procedural resource '{resource.Name}'. " +
                    "Was the assembly modified between builds?");
            TableMapping.CreateMapping(resourceType).Wait();
            SQLite<ProceduralResource>.Insert(resource).CountAsync().AsTask().Wait();
            return resource;
        }

        /// <inheritdoc />
        protected override void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Methods = methods;
            SQLite<ProceduralResource>.Update(_resource.ToAsyncSingleton()).CountAsync().AsTask().Wait();
        }

        /// <inheritdoc />
        protected override void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Description = newDescription;
            SQLite<ProceduralResource>.Update(_resource.ToAsyncSingleton()).CountAsync().AsTask().Wait();
        }

        /// <inheritdoc />
        protected override bool DeleteProceduralResource(IProceduralEntityResource resource)
        {
            var _resource = (ProceduralResource) resource;
            TableMapping.Drop(_resource.Type).Wait();
            SQLite<ProceduralResource>.Delete(_resource.ToAsyncSingleton()).Wait();
            return true;
        }
    }
}