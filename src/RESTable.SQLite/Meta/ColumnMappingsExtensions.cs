using System.Collections.Generic;
using System.Linq;

namespace RESTable.SQLite.Meta
{
    internal static class ColumnMappingsExtensions
    {
        public static ColumnMappings ToColumnMappings(this IEnumerable<ColumnMapping> mappings)
        {
            return new(mappings.Where(mapping => mapping is not null));
        }
    }
}