using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Starcounter;

namespace RESTar.Dynamit
{
    internal static class DynamitControl
    {
        internal static readonly IList<Type> DynamitTypes = typeof(DDictionary)
            .GetConcreteSubclasses()
            .Where(c => c.Namespace == "RESTar.Dynamit")
            .OrderBy(i => i.FullName)
            .ToList();

        internal static int MaxTables = DynamitTypes.Count;

        internal static Type AllocateNewTable(string alias)
        {
            var newTable = DynamitTypes.FirstOrDefault(ResourceMapping.NotExists);
            if (newTable == null)
                throw new NoAvalailableDynamicTableException();
            new ResourceMapping
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