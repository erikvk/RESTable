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
        private const string syntax = @"CREATE +INDEX +""*(?<name>\w+)""* +ON +""*(?<table>[\w\$]+)""* " +
                                      @"\((?:(?<columns>""*\w+""* *[""*\w+""*]*) *,* *)+\)";

        public async IAsyncEnumerable<DatabaseIndex> SelectAsync(IRequest<DatabaseIndex> request)
        {
            var sqls = new List<string>();
            async ValueTask AddRowToList(DbDataReader row) => sqls.Add(await row.GetFieldValueAsync<string>(0));
            await Database.QueryAsync("SELECT sql FROM sqlite_master WHERE type='index'", AddRowToList);
            var items = sqls.Select(sql =>
            {
                var groups = Regex.Match(sql, syntax, RegexOptions.IgnoreCase).Groups;
                var tableName = groups["table"].Value;
                var mapping = TableMapping.All.FirstOrDefault(m => m.TableName.EqualsNoCase(tableName));
                if (mapping == null) throw new Exception($"Unknown SQLite table '{tableName}'");
                return new DatabaseIndex(mapping.Resource.Name)
                {
                    Name = groups["name"].Value,
                    Columns = groups["columns"].Captures.Select(column =>
                    {
                        var (name, direction) = column.ToString().TSplit(' ');
                        return new ColumnInfo(name.Replace("\"", ""), direction.ToLower().Contains("desc"));
                    }).ToArray()
                };
            });
            foreach (var item in items)
                yield return item;
        }

        public async Task<int> InsertAsync(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            await foreach (var index in request.GetInputEntitiesAsync())
            {
                var tableMapping = TableMapping.GetTableMapping(index.Resource.Type);
                if (index.Resource == null)
                    throw new Exception("Found no resource to register index on");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "ASC")}"))})";
                await Database.QueryAsync(sql);
                count += 1;
            }
            return count;
        }

        public async Task<int> UpdateAsync(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            await foreach (var index in request.GetInputEntitiesAsync())
            {
                var tableMapping = TableMapping.GetTableMapping(index.Resource.Type);
                await Database.QueryAsync($"DROP INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName}");
                var sql = $"CREATE INDEX {index.Name.Fnuttify()} ON {tableMapping.TableName} " +
                          $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})";
                await Database.QueryAsync(sql);
                count += 1;
            }
            return count;
        }

        public async Task<int> DeleteAsync(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            await foreach (var index in request.GetInputEntitiesAsync())
            {
                await Database.QueryAsync($"DROP INDEX {index.Name.Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}