using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Linq;
using RESTar.Operations;
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

        internal static Type GetByTableName(string tableId) => DynamitTypes
            .FirstOrDefault(t => t.FullName == tableId);

        internal static void ClearTable(string tableId) => Db
            .SQL<DDictionary>($"SELECT t FROM {tableId} t")
            .ForEach(dict => Transact.Trans(dict.Delete));
    }
}