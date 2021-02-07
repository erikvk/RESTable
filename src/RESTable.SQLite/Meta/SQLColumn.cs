using System;
using System.Data;
using System.Threading.Tasks;
using RESTable.Resources;
using static System.StringComparison;

namespace RESTable.SQLite.Meta
{
    /// <summary>
    /// Represents a column in a SQL table
    /// </summary>
    public class SQLColumn
    {
        private ColumnMapping Mapping { get; set; }

        /// <summary>
        /// The name of the column
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of the column, as defined in SQL
        /// </summary>
        public SQLDataType Type { get; }

        /// <summary>
        /// The type of the column, as defined in System.Data
        /// </summary>
        internal DbType? DbType { get; }

        /// <summary>
        /// Does this instance represent the RowId SQLite column?
        /// </summary>
        [RESTableMember(ignore: true)] public bool IsRowId { get; }

        /// <summary>
        /// Creates a new SQLColumn instance
        /// </summary>
        public SQLColumn(string name, SQLDataType type)
        {
            Name = name;
            IsRowId = name.EqualsNoCase("rowid");
            Type = type;
            DbType = type.ToDbTypeCode();
        }

        internal void SetMapping(ColumnMapping mapping) => Mapping = mapping;

        internal async Task Push()
        {
            if (Mapping == null)
                throw new InvalidOperationException($"Cannot push the unmapped SQL column '{Name}' to the database");
            foreach (var column in await Mapping.TableMapping.GetSqlColumns())
            {
                if (column.Equals(this)) return;
                if (string.Equals(Name, column.Name, OrdinalIgnoreCase))
                    throw new SQLiteException($"Cannot push column '{Name}' to SQLite table '{Mapping.TableMapping.TableName}'. " +
                                              $"The table already contained a column definition '({column.ToSql()})'.");
            }
            await Database.QueryAsync($"BEGIN TRANSACTION;ALTER TABLE {Mapping.TableMapping.TableName} ADD COLUMN {ToSql()};COMMIT;");
        }

        internal string ToSql() => $"{Name.Fnuttify()} {Type}";

        /// <inheritdoc />
        public override string ToString() => ToSql();

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is SQLColumn col
                                                   && string.Equals(Name, col.Name, OrdinalIgnoreCase)
                                                   && Type == col.Type;

        /// <inheritdoc />
        public override int GetHashCode() => (Name.ToUpperInvariant(), Type).GetHashCode();
    }
}