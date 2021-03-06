﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTable.SQLite.Meta
{
    internal class NoCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
        public int GetHashCode(string obj) => obj.ToLower().GetHashCode();
    }
    
    /// <inheritdoc />
    /// <summary>
    /// A collection of ColumnMapping instances, indexed on CLR property name
    /// </summary>
    public class ColumnMappings : Dictionary<string, ColumnMapping>
    {
        /// <inheritdoc />
        public ColumnMappings(IEnumerable<ColumnMapping> collection) : base(new NoCaseComparer())
        {
            foreach (var item in collection) 
                this[item.CLRProperty.Name] = item;
        }

        internal string ToSQL() => string.Join(", ", Values.Where(m => !m.IsRowId).Select(c => c.SQLColumn.ToSql()));

        internal async Task Push()
        {
            foreach (var mapping in Values)
                await mapping.Push().ConfigureAwait(false);
        }
    }
}