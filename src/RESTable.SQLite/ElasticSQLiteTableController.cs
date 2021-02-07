﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources.Operations;
using RESTable.SQLite.Meta;

namespace RESTable.SQLite
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <summary>
    /// Defines a controller for a given elastic SQLite table mapping
    /// </summary>
    /// <typeparam name="TController"></typeparam>
    /// <typeparam name="TTable"></typeparam>
    public class ElasticSQLiteTableController<TController, TTable> : IAsyncSelector<TController>, IAsyncUpdater<TController>
        where TTable : ElasticSQLiteTable
        where TController : ElasticSQLiteTableController<TController, TTable>, new()
    {
        private TableMapping TableMapping { get; set; }

        /// <summary>
        /// The name of the CLR type of the elastic SQLite table mapping
        /// </summary>
        public string CLRTypeName { get; private set; }

        /// <summary>
        /// The name of the SQL table of the elastic SQLite table mapping
        /// </summary>
        public string SQLTableName { get; private set; }

        /// <summary>
        /// The column definitions for this table mapping, including dynamic members
        /// </summary>
        public Dictionary<string, CLRDataType> Columns { get; private set; }

        /// <summary>
        /// Add column names to this array to drop them from the table mapping, as well as the SQL table
        /// </summary>
        public string[] DroppedColumns { get; set; }

        /// <inheritdoc />
        public virtual Task<IEnumerable<TController>> SelectAsync(IRequest<TController> request) => Task.FromResult(Select());

        /// <inheritdoc />
        public virtual async Task<int> UpdateAsync(IRequest<TController> request)
        {
            var inputEntities = await request.GetInputEntities();
            var count = 0;
            foreach (var entity in inputEntities)
            {
                if (await entity.Update())
                    count += 1;
            }
            return count;
        }

        protected ElasticSQLiteTableController() { }

        /// <summary>
        /// Selects all elastic table mappings with CLR classes that are subtypes of TTable
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TController> Select() => TableMapping.All
            .Where(mapping => typeof(TTable).IsAssignableFrom(mapping.CLRClass))
            .Select(mapping => new TController
            {
                TableMapping = mapping,
                CLRTypeName = mapping.CLRClass.FullName,
                SQLTableName = mapping.TableName,
                Columns = mapping.ColumnMappings.ToDictionary(
                    keySelector: columnMapping => columnMapping.CLRProperty.Name,
                    elementSelector: columnMapping => columnMapping.CLRProperty.Type),
                DroppedColumns = new string[0]
            });

        /// <summary>
        /// Drops a list of columns (by name) from this elastic table mapping, as well as from the SQL table
        /// </summary>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        protected async Task<bool> DropColumns(params string[] columnNames)
        {
            var toDrop = columnNames
                .Select(columnName =>
                {
                    var mapping = TableMapping.ColumnMappings.FirstOrDefault(cm => cm.CLRProperty.Name.EqualsNoCase(columnName));
                    if (mapping == null) return null;
                    if (mapping.IsRowId || mapping.CLRProperty.IsDeclared)
                        throw new SQLiteException($"Cannot drop column '{mapping.SQLColumn.Name}' from table '{TableMapping.TableName}'. " +
                                                  "Column is not editable.");
                    return mapping;
                })
                .Where(mapping => mapping != null)
                .ToList();
            if (!toDrop.Any()) return false;
            await TableMapping.DropColumns(toDrop);
            return true;
        }

        /// <summary>
        /// Updates the column definition and pushes it to the SQL table
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Update()
        {
            var updated = false;
            var columnsToAdd = Columns.Keys
                .Except(TableMapping.SQLColumnNames)
                .Select(name => (name, type: Columns[name]));
            await DropColumns(DroppedColumns);
            foreach (var (name, type) in columnsToAdd.Where(c => c.type != CLRDataType.Unsupported))
            {
                TableMapping.ColumnMappings.Add(new ColumnMapping
                (
                    tableMapping: TableMapping,
                    clrProperty: new CLRProperty(name, type),
                    sqlColumn: new SQLColumn(name, type.ToSQLDataType())
                ));
                updated = true;
            }
            await TableMapping.ColumnMappings.Push();
            await TableMapping.Update();
            return updated;
        }
    }
}