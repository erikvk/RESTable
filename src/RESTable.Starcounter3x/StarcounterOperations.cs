using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RESTable.Admin;
using RESTable.Meta;
using RESTable.Requests;
using Starcounter.Database.Data;

namespace RESTable.Starcounter3x
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        //private const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        private static readonly string TableName = typeof(T).GetRESTableTypeName();
        private static readonly string select = $"SELECT t FROM {TableName.Fnuttify()} t ";
        private const string ObjectNo = nameof(ObjectNo);

        private static Transaction Transaction => Transaction.Current ?? Transaction.Create();

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            switch (request.Conditions.Count)
            {
                case 0:
                    var sql = $"{select}";
                    QueryConsole.Publish(sql, CancellationToken.None);
                    foreach (var item in Transaction.Run(db => db.Sql<T>(sql)))
                        yield return item;
                    yield break;
                case 1 when request.Conditions[0] is Condition<T> {Operator: Operators.EQUALS} only:
                    if (string.Equals(ObjectNo, only.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var objectNo = 0UL;
                        try
                        {
                            objectNo = (ulong) only.Value!;
                        }
                        catch (InvalidCastException)
                        {
                            // ignore
                        }
                        QueryConsole.Publish($"FROMID {objectNo}", CancellationToken.None);
                        if (objectNo == 0UL)
                            yield break;
                        yield return Transaction.Run(db => db.Get<T>(objectNo));
                        yield break;
                    }
                    else goto case default;
                default:
                    yield break;
//                    TODO This is broken and has to be fixed:
//                    string? orderBy = null;
//                    var (where, values) = request.Conditions.GetSQL().MakeWhereClause(null, out var useOrderBy);
//                    sql = useOrderBy ? $"{select}{where}{orderBy}" : $"{select}{where}";
//                    QueryConsole.Publish(sql, values);
//                    if (request.Conditions.HasPost(out var post))
//                        request.Conditions = post;
//                    foreach (var item in Transaction.Run(db => db.Sql<T>(sql, values)))
//                        yield return item;
//                    yield break;
            }
        }

        /// <summary>
        /// Inserts entities into a Starcounter table. Since 
        /// </summary>
        public static IEnumerable<T> Insert(IRequest<T> request)
        {
            return Transaction.Run(_ => request.GetInputEntities().ToList());
        }

        /// <summary>
        /// Updates entities in a Starcounter table. 
        /// </summary>
        public static IEnumerable<T> Update(IRequest<T> request)
        {
            return Transaction.Run(_ => request.GetInputEntities().ToList());
        }

        /// <summary>
        /// Deletes entities from a Starcounter table
        /// </summary>
        public static int Delete(IRequest<T> request)
        {
            var count = 0;
            Transaction.Run(db =>
            {
                foreach (var entity in request.GetInputEntities())
                {
                    db.Delete(entity);
                    count += 1;
                }
            });
            return count;
        }

        internal static bool IsValid(IEntityResource resource, TypeCache typeCache, out string? reason)
        {
            if (resource.InterfaceType is not null)
            {
                var interfaceName = resource.InterfaceType.GetRESTableTypeName();
                var members = typeCache.GetDeclaredProperties(resource.InterfaceType);
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

            reason = null;
            return true;
        }
    }
}