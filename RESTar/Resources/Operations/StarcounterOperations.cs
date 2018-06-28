using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
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
        private static readonly string SELECT = $"SELECT t FROM {typeof(T).RESTarTypeName().Fnuttify()} t ";

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            switch (request)
            {
                case var external:
                    switch (external.Conditions.Count)
                    {
                        case 0: return Db.SQL<T>($"{SELECT}");
                        case 1 when external.Conditions[0] is var only && only.Operator == Operators.EQUALS:
                            if (string.Equals("objectno", only.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var objectNo = (ulong?) only.Value;
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
                                    if (objectID == null) return null;
                                    var result = Db.FromId<T>(objectID);
                                    return result == null ? null : new[] {result};
                                }
                                catch
                                {
                                    throw new Exception("Invalid ObjectID format. Should encode a positive integer");
                                }
                            }
                            break;
                    }
                    var (whereString, values) = external.Conditions.GetSQL().MakeWhereClause();
                    var r2 = Db.SQL<T>($"{SELECT}{whereString}", values);
                    return !external.Conditions.HasPost(out var post) ? r2 : r2.Where(post);
            }
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