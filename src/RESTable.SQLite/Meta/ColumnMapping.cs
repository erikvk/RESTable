using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.Sqlite.Meta
{
    /// <summary>
    /// Defines the column definition belonging to a declared CLR member
    /// </summary>
    public class ColumnMapping
    {
        [RESTableMember(hide: true)]
        internal TableMapping TableMapping { get; }

        /// <summary>
        /// The CLR property of the mapping
        /// </summary>
        public ClrProperty ClrProperty { get; }

        /// <summary>
        /// The Sql column of the mapping
        /// </summary>
        public SqlColumn SqlColumn { get; }

        /// <summary>
        /// Dows this column mapping refer to the RowId Sqlite column?
        /// </summary>
        public bool IsRowId { get; }

        internal async Task Push()
        {
            if (IsRowId) return;
            await SqlColumn.Push().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a column from a CLR PropertyInfo
        /// </summary>
        internal ColumnMapping(TableMapping tableMapping, ClrProperty clrProperty, SqlColumn sqlColumn)
        {
            TableMapping = tableMapping;
            ClrProperty = clrProperty;
            SqlColumn = sqlColumn;
            IsRowId = sqlColumn.IsRowId;
            SqlColumn.SetMapping(this);
            ClrProperty.SetMapping(this);
        }
    }
}