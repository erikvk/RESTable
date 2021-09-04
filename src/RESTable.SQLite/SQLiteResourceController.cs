using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Sqlite
{
    /// <inheritdoc />
    /// <summary>
    /// Defines a resourcecontroller for inserting procedural (runtime) SQLite resources and modifying their
    /// column definitions
    /// </summary>
    /// <typeparam name="TController"></typeparam>
    /// <typeparam name="TBaseType"></typeparam>
    public abstract class SqliteResourceController<TController, TBaseType> : ResourceController<TController, SqliteEntityResourceProvider>
        where TController : SqliteResourceController<TController, TBaseType>, new()
        where TBaseType : ElasticSqliteTable
    {
        /// <inheritdoc />
        /// <summary>
        /// The ElasticSQLiteTableController used for retreiving and modifying the table definition
        /// of generated procedural SQLite resources
        /// </summary>
        public class TableDefinition : ElasticSqliteTableController<TableDefinition, TBaseType> { }

        /// <inheritdoc />
        protected override dynamic Data { get; }

        private Type BaseType { get; }

        /// <summary>
        /// The table definition for this procedural SQLite resource
        /// </summary>
        [RESTableMember(order: 100)]
        public TableDefinition Definition { get; private set; } = null!;

        /// <inheritdoc />
        public override IEnumerable<TController> Select(IRequest<TController> request) => base
            .Select(request)
            .Where(item => item.Type?.IsSubclassOf(BaseType) == true)
            .Select(item =>
            {
                item.Definition = TableDefinition.Select().First(s => s.ClrTypeName == item.Name);
                return item;
            });

        /// <inheritdoc />
        public override async IAsyncEnumerable<TController> UpdateAsync(IRequest<TController> request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var resource in await request.GetInputEntitiesAsync().ToListAsync(cancellationToken).ConfigureAwait(false))
            {
                resource.Update(request.Context);
                await resource.Definition.Update().ConfigureAwait(false);
                yield return resource;
            }
        }

        /// <inheritdoc />
        protected SqliteResourceController()
        {
            BaseType = typeof(TBaseType);
            if (!BaseType.IsSubclassOf(typeof(ElasticSqliteTable)))
                throw new SqliteException($"Cannot create procedural Sqlite resource from base type '{BaseType}'. Must be a " +
                                          "subclass of RESTable.Sqlite.ElasticSqliteTable with at least one defined column property.");
            Data = new SqliteProceduralResourceData {BaseTypeName = BaseType.AssemblyQualifiedName};
        }
    }
}