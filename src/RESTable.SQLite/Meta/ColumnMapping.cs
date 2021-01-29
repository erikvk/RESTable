﻿using RESTable.Resources;

namespace RESTable.SQLite.Meta
{
    /// <summary>
    /// Defines the column definition belonging to a declared CLR member
    /// </summary>
    public class ColumnMapping
    {
        [RESTableMember(hide: true)] internal TableMapping TableMapping { get; }

        /// <summary>
        /// The CLR property of the mapping
        /// </summary>
        public CLRProperty CLRProperty { get; }

        /// <summary>
        /// The SQL column of the mapping
        /// </summary>
        public SQLColumn SQLColumn { get; }

        /// <summary>
        /// Dows this column mapping refer to the RowId SQLite column?
        /// </summary>
        public bool IsRowId { get; }

        internal void Push()
        {
            if (IsRowId) return;
            SQLColumn.Push();
        }

        /// <summary>
        /// Creates a column from a CLR PropertyInfo
        /// </summary>
        internal ColumnMapping(TableMapping tableMapping, CLRProperty clrProperty, SQLColumn sqlColumn)
        {
            TableMapping = tableMapping;
            CLRProperty = clrProperty;
            SQLColumn = sqlColumn;
            IsRowId = sqlColumn.IsRowId;
            SQLColumn?.SetMapping(this);
            CLRProperty?.SetMapping(this);
        }
    }
}