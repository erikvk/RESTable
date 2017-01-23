using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Starcounter;

namespace RESTar
{
    internal static class DynamitControl
    {
        internal static readonly IList<Type> DynamitTypes = typeof(DDictionary)
            .GetConcreteSubclasses()
            .Where(c => c.HasAttribute<DynamicTableAttribute>())
            .OrderBy(i => i.FullName)
            .ToList();

        internal static int MaxTables = DynamitTypes.Count;

        internal static Type AllocateNewTable(string alias)
        {
            var newTable = DynamitTypes.FirstOrDefault(ResourceAlias.NotExists);
            if (newTable == null)
                throw new NoAvalailableDynamicTableException();
            new ResourceAlias
            {
                Alias = alias,
                Resource = newTable.FullName
            };
            return newTable;
        }

        internal static Type GetByTableId(string tableId)
        {
            return DynamitTypes.FirstOrDefault(t => t.FullName == tableId);
        }

        internal static void ClearTable(string tableId)
        {
            var dicts = Db.SQL<DDictionary>($"SELECT t FROM {tableId} t");
            foreach (var dict in dicts)
                Db.Transact(() => { dict.Delete(); });
        }
    }
}