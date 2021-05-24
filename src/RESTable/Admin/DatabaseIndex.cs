using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

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
                if (value is null)
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
                var resourceCollection = ApplicationServicesAccessor.ResourceCollection;
                if (!resourceCollection.TryFindResource<IEntityResource>(value, out var resource, out var error))
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

        /// <inheritdoc />
        public IEnumerable<InvalidMember> Validate(DatabaseIndex entity, RESTableContext context)
        {
            if (string.IsNullOrWhiteSpace(entity.ResourceName))
            {
                yield return this.Invalidate(e => e.ResourceName, "Index resource name cannot be null or whitespace");
            }
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                yield return this.Invalidate(e => e.Name, "Index name cannot be null or whitespace");
            }
            if (entity.Columns?.Any() != true)
            {
                yield return this.Invalidate(e => e.Columns, "No columns specified for index");
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DatabaseIndex> SelectAsync(IRequest<DatabaseIndex> request)
        {
            var entityResourceProviders = request
                .GetRequiredService<ResourceFactory>()
                .EntityResourceProviders
                .Values;
            foreach (var indexer in entityResourceProviders
                .Select(p => p.DatabaseIndexer)
                .Where(indexer => indexer is not null)
                .Distinct())
            await foreach (var index in indexer.SelectAsync(request).ConfigureAwait(false))
                yield return index;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DatabaseIndex> InsertAsync(IRequest<DatabaseIndex> request)
        {
            var entities = request.GetInputEntitiesAsync();
            if (entities is null) yield break;
            var entityResourceProviders = request
                .GetRequiredService<ResourceFactory>()
                .EntityResourceProviders;
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider).ConfigureAwait(false))
            {
                if (!entityResourceProviders.TryGetValue(@group.Key, out var provider) || provider.DatabaseIndexer is not IDatabaseIndexer indexer)
                    throw new Exception($"Unable to register index. Resource '{(await group.FirstAsync().ConfigureAwait(false)).Resource.Name}' is not a database resource.");
                request.Selector = () => group;
                await foreach (var index in indexer.InsertAsync(request).ConfigureAwait(false))
                    yield return index;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DatabaseIndex> UpdateAsync(IRequest<DatabaseIndex> request)
        {
            var entities = request.GetInputEntitiesAsync();
            if (entities is null) yield break;
            var entityResourceProviders = request
                .GetRequiredService<ResourceFactory>()
                .EntityResourceProviders;
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider).ConfigureAwait(false))
            {
                request.Updater = _ => group;
                await foreach (var index in entityResourceProviders[@group.Key].DatabaseIndexer.UpdateAsync(request).ConfigureAwait(false))
                    yield return index;
            }
        }

        /// <inheritdoc />
        public async ValueTask<int> DeleteAsync(IRequest<DatabaseIndex> request)
        {
            var count = 0;
            var entities = request.GetInputEntitiesAsync();
            var entityResourceProviders = request
                .GetRequiredService<ResourceFactory>()
                .EntityResourceProviders;
            await foreach (var group in entities.GroupBy(index => index.Resource.Provider).ConfigureAwait(false))
            {
                request.Selector = () => group;
                count += await entityResourceProviders[@group.Key].DatabaseIndexer.DeleteAsync(request).ConfigureAwait(false);
            }
            return count;
        }
    }
}