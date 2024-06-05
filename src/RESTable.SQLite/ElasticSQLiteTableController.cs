using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources.Operations;
using RESTable.Sqlite.Meta;

namespace RESTable.Sqlite;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <inheritdoc cref="IAsyncUpdater{T}" />
/// <summary>
///     Defines a controller for a given elastic Sqlite table mapping
/// </summary>
/// <typeparam name="TController"></typeparam>
/// <typeparam name="TTable"></typeparam>
public class ElasticSqliteTableController<TController, TTable> : IAsyncSelector<TController>, IAsyncUpdater<TController>
    where TTable : ElasticSqliteTable
    where TController : ElasticSqliteTableController<TController, TTable>, new()
{
    protected ElasticSqliteTableController()
    {
        TableMapping = null!;
        ClrTypeName = null!;
        SqlTableName = null!;
        Columns = null!;
        DroppedColumns = null!;
    }

    private TableMapping TableMapping { get; set; }

    /// <summary>
    ///     The name of the CLR type of the elastic Sqlite table mapping
    /// </summary>
    public string ClrTypeName { get; private set; }

    /// <summary>
    ///     The name of the Sql table of the elastic Sqlite table mapping
    /// </summary>
    public string SqlTableName { get; private set; }

    /// <summary>
    ///     The column definitions for this table mapping, including dynamic members
    /// </summary>
    public Dictionary<string, ClrDataType> Columns { get; private set; }

    /// <summary>
    ///     Add column names to this array to drop them from the table mapping, as well as the Sqlite table
    /// </summary>
    public string[] DroppedColumns { get; set; }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<TController> SelectAsync(IRequest<TController> request, CancellationToken cancellationToken)
    {
        return Select().ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<TController> UpdateAsync(IRequest<TController> request, CancellationToken cancellationToken)
    {
        return request
            .GetInputEntitiesAsync()
            .WhereAwait(async entity => await entity.Update().ConfigureAwait(false));
    }

    /// <summary>
    ///     Selects all elastic table mappings with CLR classes that are subtypes of TTable
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<TController> Select()
    {
        return TableMapping.All
            .Where(mapping => typeof(TTable).IsAssignableFrom(mapping.ClrClass))
            .Select(mapping => new TController
            {
                TableMapping = mapping,
                ClrTypeName = mapping.ClrClass.FullName!,
                SqlTableName = mapping.TableName,
                Columns = mapping.ColumnMappings!.Values.ToDictionary(
                    columnMapping => columnMapping.ClrProperty.Name,
                    columnMapping => columnMapping.ClrProperty.Type),
                DroppedColumns = []
            });
    }

    /// <summary>
    ///     Drops a list of columns (by name) from this elastic table mapping, as well as from the Sqlite table
    /// </summary>
    /// <param name="columnNames"></param>
    /// <returns></returns>
    protected async Task<bool> DropColumns(params string[] columnNames)
    {
        IEnumerable<ColumnMapping> getMappings()
        {
            foreach (var columnName in columnNames)
            {
                var mapping = TableMapping.ColumnMappings!.Values.FirstOrDefault(cm => cm.ClrProperty.Name.EqualsNoCase(columnName));
                if (mapping is null) continue;
                if (mapping.IsRowId || mapping.ClrProperty.IsDeclared)
                    throw new SqliteException($"Cannot drop column '{mapping.SqlColumn.Name}' from table '{TableMapping.TableName}'. " +
                                              "Column is not editable.");
                yield return mapping;
            }
        }

        var toDrop = getMappings().ToList();
        if (!toDrop.Any()) return false;
        await TableMapping.DropColumns(toDrop).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Updates the column definition and pushes it to the Sqlite table
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Update()
    {
        var updated = false;
        var columnsToAdd = Columns.Keys
            .Except(TableMapping.SqlColumnNames)
            .Select(name => (name, type: Columns[name]));
        await DropColumns(DroppedColumns).ConfigureAwait(false);
        foreach (var (name, type) in columnsToAdd.Where(c => c.type != ClrDataType.Unsupported))
        {
            TableMapping.ColumnMappings![name] = new ColumnMapping
            (
                TableMapping,
                new ClrProperty(name, type),
                new SqlColumn(name, type.ToSqlDataType())
            );
            updated = true;
        }
        await TableMapping.ColumnMappings!.Push().ConfigureAwait(false);
        await TableMapping.Update().ConfigureAwait(false);
        return updated;
    }
}
