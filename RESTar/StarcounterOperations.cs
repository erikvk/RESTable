using System;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Reflection.Dynamic;
using RESTar.Resources;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        internal const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        internal static readonly string SELECT = $"SELECT t FROM {typeof(T).RESTarTypeName().Fnuttify()} t ";

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static Selector<T> Select { get; }

        /// <summary>
        /// Inserts entities into a Starcounter table. Since 
        /// </summary>
        public static Inserter<T> Insert { get; }

        /// <summary>
        /// Updates entities in a Starcounter table. 
        /// </summary>
        public static Updater<T> Update { get; }

        /// <summary>
        /// Deletes entities from a Starcounter table
        /// </summary>
        public static Deleter<T> Delete { get; }

        /// <summary>
        /// Creates profiles for Starcounter tables
        /// </summary>
        public static Profiler<T> Profile { get; }

        static StarcounterOperations()
        {
            Select = r =>
            {
                switch (r)
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
                                        var objectNo = (ulong) only.Value;
                                        var result = Db.FromId<T>(objectNo);
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
                                        var result = Db.FromId<T>(objectID);
                                        return result == null ? null : new[] {result};
                                    }
                                    catch
                                    {
                                        throw new Exception("Invalid ObjectNo format. Should be positive integer");
                                    }
                                }
                                break;
                        }
                        var (whereString, values) = external.Conditions.GetSQL().MakeWhereClause();
                        var r2 = Db.SQL<T>($"{SELECT}{whereString}", values);
                        return !external.Conditions.HasPost(out var post) ? r2 : r2.Where(post);
                }
            };
            Insert = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetInputEntities().Count());
                return count;
            };
            Update = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetInputEntities().Count());
                return count;
            };
            Delete = r =>
            {
                var count = 0;
                Db.TransactAsync(() => r.GetInputEntities().ForEach(entity =>
                {
                    entity.Delete();
                    count += 1;
                }));
                return count;
            };
            Profile = r => ResourceProfile.Make(r, rows =>
            {
                var resourceSQLName = typeof(T).RESTarTypeName();
                var scColumns = Db.SQL<Column>(ColumnByTable, resourceSQLName).Select(c => c.Name).ToList();
                var columns = r.Members.Values.Where(p => scColumns.Contains(p.Name)).ToList();
                return rows.Sum(e => columns.Sum(p => p.ByteCount(e)) + 16);
            });
        }

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

            if (resource.Type.Implements(typeof(IProfiler<>)))
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