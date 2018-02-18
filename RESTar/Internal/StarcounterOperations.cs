using System;
using System.Linq;
using RESTar.Admin;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using Starcounter.Metadata;
using static System.StringComparison;
using static RESTar.Operators;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    internal static class StarcounterOperations<T> where T : class
    {
        internal const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        internal static readonly string SELECT = $"SELECT t FROM {typeof(T).FullName.Fnuttify()} t ";
        internal static readonly string COUNT = $"SELECT COUNT(t) FROM {typeof(T).FullName.Fnuttify()} t ";

        internal static readonly Selector<T> Select;
        internal static readonly Inserter<T> Insert;
        internal static readonly Updater<T> Update;
        internal static readonly Deleter<T> Delete;
        internal static readonly Profiler<T> Profile;
        internal static readonly Counter<T> Count;

        static StarcounterOperations()
        {
            Select = r =>
            {
                switch (r)
                {
                    case Request<T> @internal:
                        var r1 = Db.SQL<T>(@internal.SelectQuery, @internal.SqlValues);
                        return !@internal.Conditions.HasPost(out var _post) ? r1 : r1.Where(_post);
                    case var external:
                        switch (external.Conditions.Length)
                        {
                            case 0: return Db.SQL<T>($"{SELECT}{external.MetaConditions.OrderBy?.SQL}");
                            case 1 when external.Conditions[0] is var only && only.Operator == EQUALS:
                                if (string.Equals("objectno", only.Key, OrdinalIgnoreCase))
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
                                if (string.Equals("objectid", only.Key, OrdinalIgnoreCase))
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
                        var r2 = Db.SQL<T>($"{SELECT}{whereString} {external.MetaConditions.OrderBy?.SQL}", values);
                        return !external.Conditions.HasPost(out var post) ? r2 : r2.Where(post);
                }
            };
            Insert = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetEntities().Count());
                return count;
            };
            Update = r =>
            {
                var count = 0;
                Db.TransactAsync(() => count = r.GetEntities().Count());
                return count;
            };
            Delete = r =>
            {
                var count = 0;
                Db.TransactAsync(() => r.GetEntities().ForEach(entity =>
                {
                    entity.Delete();
                    count += 1;
                }));
                return count;
            };
            Count = r =>
            {
                switch (r)
                {
                    case Request<T> @internal when @internal.Conditions.HasPost(out var _): return Select(r)?.LongCount() ?? 0L;
                    case Request<T> @internal: return Db.SQL<long>(@internal.CountQuery, @internal.SqlValues).FirstOrDefault();
                    case var external when external.MetaConditions.Distinct != null:
                        return external.MetaConditions.Distinct.Apply(Select(r))?.LongCount() ?? 0L;
                    case var external when !external.Conditions.Any(): return Db.SQL<long>(COUNT).FirstOrDefault();
                    case var external when external.Conditions.HasPost(out var _): return Select(r)?.LongCount() ?? 0L;
                    case var external:
                        var (whereString, values) = external.Conditions.GetSQL().MakeWhereClause();
                        return Db.SQL<long>($"{COUNT}{whereString}", values).FirstOrDefault();
                }
            };
            Profile = r => ResourceProfile.Make(r, rows =>
            {
                var resourceSQLName = typeof(T).FullName;
                var scColumns = Db.SQL<Column>(ColumnByTable, resourceSQLName).Select(c => c.Name).ToList();
                var columns = r.Members.Values.Where(p => scColumns.Contains(p.Name)).ToList();
                return rows.Sum(e => columns.Sum(p => p.ByteCount(e)) + 16);
            });
        }

        internal static bool IsValid(IEntityResource resource, out string reason)
        {
            var item = resource.Type;
            if (item.HasAttribute<RESTarAttribute>(out var a) && a.Interface is Type i)
            {
                var members = i.GetDeclaredProperties();
                if (members.ContainsKey("objectno"))
                {
                    reason = $"Invalid Interface '{i.FullName}' assigned to resource '{resource.Name}'. " +
                             "Interface contained a property with a reserved name: 'ObjectNo'";
                    return false;
                }
                if (members.ContainsKey("objectid"))
                {
                    reason = $"Invalid Interface '{i.FullName}' assigned to resource '{resource.Name}'. " +
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