using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar.Resources
{
    internal class StarcounterIndexer : IDatabaseIndexer
    {
        private const string ColumnSql = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                         "WHERE t.\"Index\" =? ORDER BY t.Position";

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return Db.SQL<Index>("SELECT t FROM Starcounter.Metadata.\"Index\" t")
                .Where(index => !index.Table.FullName.StartsWith("Starcounter."))
                .Where(index => !index.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
                .Select(index => new DatabaseIndex(Resource.ByTypeName(index.Table.FullName)?.Name)
                {
                    Name = index.Name,
                    Columns = Db.SQL<IndexedColumn>(ColumnSql, index).Select(c => new ColumnInfo
                    {
                        Name = c.Column.Name,
                        Descending = c.Ascending == 0
                    }).ToArray()
                })
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                if (index.IResource == null)
                    throw new Exception("Found no resource to register index on");
                Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()}");
                Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}