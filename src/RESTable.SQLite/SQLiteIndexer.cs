using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RESTable.Admin;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.SQLite
{
    internal class SQLiteIndexer : IDatabaseIndexer
    {
        private const string CreateIndexRegexPattern = @"CREATE +INDEX +""*(?<name>\w+)""* +ON +""*(?<table>[\w\$]+)""* " +
                                                       @"\((?:(?<columns>""*\w+""* *[""*\w+""*]*) *,* *)+\)";

        private Query SelectIndexQuery { get; }

        public SQLiteIndexer()
        {
            SelectIndexQuery = new Query("SELECT sql FROM sqlite_master WHERE type='index'");
        }

        public async IAsyncEnumerable<DatabaseIndex> SelectAsync(IRequest<DatabaseIndex> request)
        {
            await foreach (var row in SelectIndexQuery.GetRows().ConfigureAwait(false))
            {
                var sql = await row.GetFieldValueAsync<string>(0).ConfigureAwait(false);
                var groups = Regex.Match(sql, CreateIndexRegexPattern, RegexOptions.IgnoreCase).Groups;
                var tableName = groups["table"].Value;
                var mapping = TableMapping.All.FirstOrDefault(m => m.TableName.EqualsNoCase(tableName));
                if (mapping is null) throw new Exception($"Unknown SQLite table '{tableName}'");
                yield return new DatabaseIndex
                {
                    ResourceName = mapping.Resource.Name,
                    Name = groups["name"].Value,
                    Columns = groups["columns"].Captures.Select(column =>
                    {
                        var (name, direction) = column.ToString().TSplit(' ');
                        return new ColumnInfo
                        (
                            name: name.Replace("\"", ""),
                            descending: direction.ToLower().Contains("desc")
                        );
                    }).ToArray()
                };
            }
        }

        public async IAsyncEnumerable<DatabaseIndex> InsertAsync(IRequest<DatabaseIndex> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            await foreach (var index in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                var tableMapping = TableMapping.GetTableMapping(index.Resource.Type);
                if (index.Resource is null)
                    throw new Exception("Found no resource to register index on");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "ASC")}"))})";
                var query = new Query(sql);
                await query.Execute().ConfigureAwait(false);
                yield return index;
            }
        }

        public async IAsyncEnumerable<DatabaseIndex> UpdateAsync(IRequest<DatabaseIndex> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            await foreach (var index in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                var tableMapping = TableMapping.GetTableMapping(index.Resource.Type);
                var dropIndexQuery = new Query($"DROP INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName}");
                await dropIndexQuery.Execute().ConfigureAwait(false);
                var createIndexSql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                                     $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})";
                var createIndexQuery = new Query(createIndexSql);
                await createIndexQuery.Execute().ConfigureAwait(false);
                yield return index;
            }
        }

        public async ValueTask<int> DeleteAsync(IRequest<DatabaseIndex> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            await foreach (var index in request.GetInputEntitiesAsync().ConfigureAwait(false))
            {
                var query = new Query($"DROP INDEX {index.Name.Fnuttify()}");
                await query.Execute().ConfigureAwait(false);
                count += 1;
            }
            return count;
        }
    }
}