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
        private const string AllIndexes = "SELECT t FROM Starcounter.Metadata.\"Index\" t";

        private const string ColumnByIndex = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                             "WHERE t.\"Index\" =? ORDER BY t.Position";

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IQuery<DatabaseIndex> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return Db.SQL<Index>(AllIndexes)
                .Where(index => !index.Table.FullName.StartsWith("Starcounter."))
                .Where(index => !index.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
                .Select(index => (resource: Resource.ByTypeName(index.Table.FullName), index))
                .Where(pair => pair.resource != null)
                .Select(pair => new DatabaseIndex(pair.resource.Name)
                {
                    Name = pair.index.Name,
                    Columns = Db.SQL<IndexedColumn>(ColumnByIndex, pair.index)
                        .Select(c => new ColumnInfo(c.Column.Name, c.Ascending == 0))
                        .ToArray()
                })
                .Where(query.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IQuery<DatabaseIndex> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var count = 0;
            foreach (var index in query.GetEntities())
            {
                if (index.IResource == null)
                    throw new Exception("Found no resource to register index on");
                try
                {
                    Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.RESTarTypeName().Fnuttify()} " +
                           $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()}{(c.Descending ? " DESC" : "")}"))})");
                    count += 1;
                }
                catch (SqlException e)
                {
                    throw new Exception($"Could not create index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.IResource.Name}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IQuery<DatabaseIndex> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var count = 0;
            foreach (var index in query.GetEntities())
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.RESTarTypeName().Fnuttify()}");
                try
                {
                    Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.RESTarTypeName().Fnuttify()} " +
                           $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
                    count += 1;
                }
                catch (SqlException e)
                {
                    throw new Exception($"Could not update index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.IResource.Name}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IQuery<DatabaseIndex> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var count = 0;
            foreach (var index in query.GetEntities())
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.RESTarTypeName().Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}