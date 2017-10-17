using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;

namespace RESTar.SQLite
{
    internal class SQLiteIndexer : IDatabaseIndexer
    {
        private const string syntax = @"CREATE INDEX (?<name>\w+) ON (?<table>\w+) \((?<columns>\w+ \w+)\)";

        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request)
        {
            var sqls = new List<string>();
            SQLiteDb.Query("SELECT sql FROM sqlite_master WHERE type='index'", row => sqls.Add(row.GetString(0)));
            return sqls.Select(sql =>
            {
                var groups = Regex.Match(sql, syntax, RegexOptions.IgnoreCase).Groups;
                return new DatabaseIndex
                {
                    Name = groups["name"].Value,
                    DatabaseTable = groups["table"].Value,
                    Resource = groups["table"].Value.GetResourceName(),
                    Columns = groups["columns"].Value.Split(',').Select(column =>
                    {
                        var (name, order) = column.TSplit(' ');
                        return new ColumnInfo
                        {
                            Name = name,
                            Descending = order == "DESC"
                        };
                    }).ToArray()
                };
            });
        }

        public int Insert(IEnumerable<DatabaseIndex> entities, IRequest<DatabaseIndex> request)
        {
            throw new NotImplementedException();
        }

        public int Update(IEnumerable<DatabaseIndex> entities, IRequest<DatabaseIndex> request)
        {
            throw new NotImplementedException();
        }

        public int Delete(IEnumerable<DatabaseIndex> entities, IRequest<DatabaseIndex> request)
        {
            throw new NotImplementedException();
        }
    }
}