using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Requests.Filters;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar.Resources.Operations
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        private const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        private static readonly string IndexedColumnByColumn = "SELECT t FROM Starcounter.Metadata.IndexedColumn t WHERE t.\"Column\" =?";
        private static readonly string TableName = typeof(T).RESTarTypeName();
        private static readonly string SELECT = $"SELECT t FROM {TableName.Fnuttify()} t ";

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            string sql;
            switch (request.Conditions.Count)
            {
                case 0:
                    sql = $"{SELECT}{GetOrderbyString(request)}";
                    QueryConsole.Publish("SC SQL", sql);
                    return Db.SQL<T>(sql);
                case 1 when request.Conditions[0] is var only && only.Operator == Operators.EQUALS:
                    if (string.Equals("objectno", only.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var objectNo = (ulong?) only.Value;
                            sql = $"FROMID {objectNo}";
                            QueryConsole.Publish("SC SQL", sql);
                            if (!objectNo.HasValue) return null;
                            var result = Db.FromId<T>(objectNo.Value);
                            return result == null ? null : new[] {result};
                        }
                        catch
                        {
                            throw new Exception("Invalid ObjectNo format. Should be positive integer");
                        }
                    }
                    if (string.Equals("objectid", only.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var objectID = (string) only.Value;
                            sql = $"FROMID {objectID}";
                            QueryConsole.Publish("SC SQL", sql);
                            if (objectID == null) return null;
                            var result = Db.FromId<T>(objectID);
                            return result == null ? null : new[] {result};
                        }
                        catch
                        {
                            throw new Exception("Invalid ObjectID format. Should be a Base64 string " +
                                                "encoding a positive integer");
                        }
                    }
                    break;
            }
            var (whereString, values) = request.Conditions.GetSQL().MakeWhereClause();
            sql = $"{SELECT}{whereString}{GetOrderbyString(request)}";
            QueryConsole.Publish("SC SQL", sql);
            var r2 = Db.SQL<T>(sql, values);
            return !request.Conditions.HasPost(out var post) ? r2 : r2.Where(post);
        }

        private static string GetOrderbyString(IRequest request)
        {
            if (request.MetaConditions.OrderBy is OrderBy orderBy
                && orderBy.Term.Count == 1
                && orderBy.Term.First is DeclaredProperty prop
                && prop.ScIndexableColumn is Column column
                && Db.SQL<IndexedColumn>(IndexedColumnByColumn, column).Any())
            {
                if (prop.Type != typeof(string)) orderBy.Skip = true;
                return orderBy.Ascending ? $"ORDER BY t.\"{prop.ActualName}\" ASC" : $"ORDER BY t.\"{prop.ActualName}\" DESC";
            }
            return null;
        }

        /// <summary>
        /// Inserts entities into a Starcounter table. Since 
        /// </summary>
        public static int Insert(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Updates entities in a Starcounter table. 
        /// </summary>
        public static int Update(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Deletes entities from a Starcounter table
        /// </summary>
        public static int Delete(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => request.GetInputEntities().ForEach(entity =>
            {
                entity.Delete();
                count += 1;
            }));
            return count;
        }

        /// <summary>
        /// Creates profiles for Starcounter tables
        /// </summary>
        public static ResourceProfile Profile(IEntityResource<T> resource) => ResourceProfile.Make(resource, rows =>
        {
            var resourceSQLName = typeof(T).RESTarTypeName();
            var scColumns = Db.SQL<Column>(ColumnByTable, resourceSQLName).Select(c => c.Name).ToList();
            var columns = resource.Members.Values.Where(p => scColumns.Contains(p.Name)).ToList();
            return rows.Sum(e => columns.Sum(p => p.ByteCount(e)) + 16);
        });

        internal static bool IsValid(IEntityResource resource, out string reason)
        {
            if (resource.InterfaceType != null)
            {
                var interfaceName = resource.InterfaceType.RESTarTypeName();
                var members = resource.InterfaceType.GetDeclaredProperties();
                if (members.ContainsKey("objectno"))
                {
                    reason = $"Invalid Interface '{interfaceName}' assigned to resource '{resource.Name}'. " +
                             "Interface contained a property with a reserved name: 'ObjectNo'";
                    return false;
                }
                if (members.ContainsKey("objectid"))
                {
                    reason = $"Invalid Interface '{interfaceName}' assigned to resource '{resource.Name}'. " +
                             "Interface contained a property with a reserved name: 'ObjectID'";
                    return false;
                }
            }

            if (resource.Type.ImplementsGenericInterface(typeof(IProfiler<>)))
            {
                reason = $"Invalid IProfiler interface implementation for resource type '{resource.Name}'. " +
                         "Starcounter database resources use their default profilers, and cannot implement IProfiler";
                return false;
            }
            reason = null;
            return true;
        }
    }
}