using System;
using System.Collections.Generic;
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

        public async Task<IEnumerable<DatabaseIndex>> SelectAsync(IRequest<DatabaseIndex> request)
        {
            var sqls = new List<string>();
            await Database.QueryAsync("SELECT sql FROM sqlite_master WHERE type='index'", row => sqls.Add(row.GetString(0)));
            return sqls.Select(sql =>
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
        }

        public async Task<int> InsertAsync(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in await request.GetInputEntities())
            {
                var tableMapping = TableMapping.Get(index.Resource.Type);
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
            foreach (var index in await request.GetInputEntities())
            {
                var tableMapping = TableMapping.Get(index.Resource.Type);
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
            foreach (var index in await request.GetInputEntities())
            {
                await Database.QueryAsync($"DROP INDEX {index.Name.Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}