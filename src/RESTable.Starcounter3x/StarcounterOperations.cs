﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Admin;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Linq;
using Starcounter.Database;
using Starcounter.Database.Data;

namespace RESTable.Starcounter3x
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        private const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        private static readonly string TableName = typeof(T).GetRESTableTypeName();
        private static readonly string select = $"SELECT t FROM {TableName.Fnuttify()} t ";
        private const string ObjectNo = nameof(ObjectNo);

        private static readonly TransactOptions ReadonlyOptions = new TransactOptions
        (
            flags: TransactionFlags.ReadOnly,
            onCommit: () => { }
        );

        private static Transaction Transaction => Transaction.Current ?? Transaction.Create();

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            request.EnsureServiceAttached(Transaction);
            switch (request.Conditions.Count)
            {
                case 0:
                    var sql = $"{select}{GetOrderbyString(request, out _)}";
                    QueryConsole.Publish(sql, null);
                    foreach (var item in Transaction.Run(db => db.Sql<T>(sql)))
                        yield return item;
                    yield break;
                case 1 when request.Conditions[0] is var only && only.Operator == Operators.EQUALS:
                    if (string.Equals(ObjectNo, only.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var objectNo = only.SafeSelect(o => (ulong) only.Value);
                        QueryConsole.Publish($"FROMID {objectNo}", null);
                        if (objectNo == 0)
                            yield break;
                        yield return Transaction.Run(db => db.Get<T>(objectNo));
                        yield break;
                    }
                    else goto case default;
                default:
                    var orderBy = GetOrderbyString(request, out var orderByIndexName);
                    var (where, values) = request.Conditions.GetSQL().MakeWhereClause(orderByIndexName, out var useOrderBy);
                    sql = useOrderBy ? $"{select}{where}{orderBy}" : $"{select}{where}";
                    QueryConsole.Publish(sql, values);
                    if (request.Conditions.HasPost(out var post))
                        request.Conditions = post;
                    foreach (var item in Transaction.Run(db => db.Sql<T>(sql, values)))
                        yield return item;
                    yield break;
            }
        }

        private static string GetOrderbyString(IRequest request, out string indexedName)
        {
            indexedName = null;
            return null;
        }

        /// <summary>
        /// Inserts entities into a Starcounter table. Since 
        /// </summary>
        public static int Insert(IRequest<T> request)
        {
            request.EnsureServiceAttached(Transaction);
            var count = 0;
            Transaction.Run(db => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Updates entities in a Starcounter table. 
        /// </summary>
        public static int Update(IRequest<T> request)
        {
            var count = 0;
            Transaction.Run(db => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Deletes entities from a Starcounter table
        /// </summary>
        public static int Delete(IRequest<T> request)
        {
            request.EnsureServiceAttached(Transaction);
            var count = 0;
            Transaction.Run(db => request.GetInputEntities().ForEach(entity =>
            {
                db.Delete(entity);
                count += 1;
            }));
            return count;
        }

        internal static bool IsValid(IEntityResource resource, out string reason)
        {
            if (resource.InterfaceType != null)
            {
                var interfaceName = resource.InterfaceType.GetRESTableTypeName();
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

            reason = null;
            return true;
        }
    }
}