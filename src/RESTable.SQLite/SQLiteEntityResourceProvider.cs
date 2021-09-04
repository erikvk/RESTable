using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.Sqlite
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
    public class SqliteEntityResourceProvider : EntityResourceProvider<SqliteTable>, IProceduralEntityResourceProvider
    {
        private static bool IsInitiated { get; set; }

        private static void Init()
        {
            if (IsInitiated) return;
            foreach (var clrClass in typeof(SqliteTable).GetConcreteSubclasses())
                TableMapping.CreateMapping(clrClass).Wait();
            IsInitiated = true;
        }

        /// <inheritdoc />
        protected override bool IsValid(IEntityResource resource, TypeCache typeCache, out string? reason)
        {
            reason = null;
            return true;
        }

        /// <inheritdoc />
        protected override void ModifyResourceAttribute(Type type, RESTableAttribute attribute)
        {
            if (type.IsSubclassOf(typeof(ElasticSqliteTable)))
            {
                attribute.AllowDynamicConditions = true;
            }
        }

        /// <summary>
        /// Creates a new instance of the SQLiteProvider class, for use in calls to RESTableConfig.Init()
        /// </summary>
        public SqliteEntityResourceProvider(IOptions<SqliteOptions> options)
        {
            var databasePath = options.Value.SqliteDatabasePath;
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new SqliteException("The Sqlite database path cannot be null, empty or whitespace");
            var (directory, fileName, extension) = (Path.GetDirectoryName(databasePath), Path.GetFileNameWithoutExtension(databasePath),
                Path.GetExtension(databasePath));
            if (string.IsNullOrWhiteSpace(extension))
            {
                if (Directory.Exists(databasePath))
                    throw new SqliteException(
                        $"The Sqlite database path '{databasePath}' was invalid. Must be an absolute file path, " +
                        "including file name. Found reference to folder.");
                databasePath += ".sqlite";
            }

            if (string.IsNullOrWhiteSpace(directory))
                throw new SqliteException(
                    $"The Sqlite database path '{databasePath}' was invalid. Must be an absolute file path, including file name.");
            if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9_]+$"))
                throw new SqliteException($"Sqlite database file name '{fileName}' contains invalid characters: " +
                                          "Only letters, numbers and underscores are valid in Sqlite database file names.");

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
            Init();
        }

        /// <inheritdoc />
        protected override void ReceiveClaimed(ICollection<IEntityResource> claimedResources)
        {
            foreach (var claimed in claimedResources)
            {
                var tableMapping = TableMapping.GetTableMapping(claimed.Type);
                if (tableMapping is null)
                {
                    // Skip this type
                    continue;
                }
                tableMapping.Resource = claimed;
            }
        }

        /// <inheritdoc />
        protected override Type AttributeType => typeof(SqliteAttribute);

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultSelectAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class
        {
            return SqliteOperations<T>.SelectAsync(request);
        }

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultInsertAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class
        {
            return SqliteOperations<T>.InsertAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override IAsyncEnumerable<T> DefaultUpdateAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class
        {
            return SqliteOperations<T>.UpdateAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask<long> DefaultDeleteAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class
        {
            return SqliteOperations<T>.DeleteAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override ValueTask<long> DefaultCountAsync<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class
        {
            return SqliteOperations<T>.CountAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override IEnumerable<IProceduralEntityResource> SelectProceduralResources(RESTableContext context)
        {
            foreach (var resource in Sqlite<ProceduralResource>.Select().ToEnumerable())
            {
                var type = resource.Type;
                if (type is not null)
                {
                    if (TableMapping.GetTableMapping(type) is null)
                        TableMapping.CreateMapping(type).Wait();
                    yield return resource;
                }
            }
        }

        /// <inheritdoc />
        protected override IProceduralEntityResource InsertProceduralResource(RESTableContext context, string name, string? description, Method[] methods, dynamic? data)
        {
            var resource = new ProceduralResource
            {
                Name = name,
                Description = description,
                Methods = methods,
                BaseTypeName = data?.BaseTypeName ?? throw new SqliteException("No BaseTypeName defined in 'Data' in resource controller")
            };
            var resourceType = resource.Type;
            if (resourceType is null)
                throw new SqliteException(
                    $"Could not locate basetype '{resource.BaseTypeName}' when building procedural resource '{resource.Name}'. " +
                    "Was the assembly modified between builds?");
            TableMapping.CreateMapping(resourceType).Wait();
            Sqlite<ProceduralResource>.Insert(resource.ToAsyncSingleton()).CountAsync().AsTask().Wait();
            return resource;
        }

        /// <inheritdoc />
        protected override void SetProceduralResourceMethods(RESTableContext context, IProceduralEntityResource resource, Method[] methods)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Methods = methods;
            Sqlite<ProceduralResource>.Update(_resource.ToAsyncSingleton()).CountAsync().AsTask().Wait();
        }

        /// <inheritdoc />
        protected override void SetProceduralResourceDescription(RESTableContext context, IProceduralEntityResource resource, string? newDescription)
        {
            var _resource = (ProceduralResource) resource;
            _resource.Description = newDescription;
            Sqlite<ProceduralResource>.Update(_resource.ToAsyncSingleton()).CountAsync().AsTask().Wait();
        }

        /// <inheritdoc />
        protected override bool DeleteProceduralResource(RESTableContext context, IProceduralEntityResource resource)
        {
            var _resource = (ProceduralResource) resource;
            TableMapping.Drop(_resource.Type).Wait();
            Sqlite<ProceduralResource>.Delete(_resource.ToAsyncSingleton()).AsTask().Wait();
            return true;
        }
    }
}