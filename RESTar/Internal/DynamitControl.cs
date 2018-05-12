using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Linq;
using Starcounter;

namespace RESTar.Internal
{
    internal static class DynamitControl
    {
        internal static readonly IList<Type> DynamitTypes = typeof(DDictionary)
            .GetConcreteSubclasses()
            .Where(c => c.HasAttribute<DynamicTableAttribute>())
            .OrderBy(i => i.RESTarTypeName())
            .ToList();

        internal static Type GetByTableName(string tableId) => DynamitTypes
            .FirstOrDefault(t => t.RESTarTypeName() == tableId);

        internal static void ClearTable(string tableId) => Db
            .SQL<DDictionary>($"SELECT t FROM {tableId} t")
            .ForEach(dict => Db.TransactAsync(dict.Delete));
    }
}