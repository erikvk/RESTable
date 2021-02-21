using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTable.SQLite.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A collection of ColumnMapping instances
    /// </summary>
    public class ColumnMappings : List<ColumnMapping>
    {
        /// <inheritdoc />
        public ColumnMappings(IEnumerable<ColumnMapping> collection) : base(collection) { }

        internal string ToSQL() => string.Join(", ", this.Where(m => !m.IsRowId).Select(c => c.SQLColumn.ToSql()));

        internal async Task Push()
        {
            foreach (var mapping in this)
                await mapping.Push().ConfigureAwait(false);
        }
    }
}