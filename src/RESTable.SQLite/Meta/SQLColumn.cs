using System;
using System.Data;
using System.Threading.Tasks;
using RESTable.Resources;
using static System.StringComparison;

namespace RESTable.Sqlite.Meta;

/// <summary>
///     Represents a column in a Sql table
/// </summary>
public class SqlColumn
{
    /// <summary>
    ///     Creates a new SqlColumn instance
    /// </summary>
    public SqlColumn(string name, SqlDataType type)
    {
        Name = name;
        IsRowId = name.EqualsNoCase("rowid");
        Type = type;
        DbType = type.ToDbTypeCode();
        Mapping = null!;
    }

    private ColumnMapping Mapping { get; set; }

    /// <summary>
    ///     The name of the column
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The type of the column, as defined in Sql
    /// </summary>
    public SqlDataType Type { get; }

    /// <summary>
    ///     The type of the column, as defined in System.Data
    /// </summary>
    internal DbType? DbType { get; }

    /// <summary>
    ///     Does this instance represent the RowId Sqlite column?
    /// </summary>
    [RESTableMember(true)]
    public bool IsRowId { get; }

    internal void SetMapping(ColumnMapping mapping)
    {
        Mapping = mapping;
    }

    internal async Task Push()
    {
        if (Mapping is null)
            throw new InvalidOperationException($"Cannot push the unmapped Sql column '{Name}' to the database");
        await foreach (var column in Mapping.TableMapping.GetSqlColumns().ConfigureAwait(false))
        {
            if (column.Equals(this)) return;
            if (string.Equals(Name, column.Name, OrdinalIgnoreCase))
                throw new SqliteException($"Cannot push column '{Name}' to SqlLite table '{Mapping.TableMapping.TableName}'. " +
                                          $"The table already contained a column definition '({column.ToSql()})'.");
        }
        var pushQuery = new Query($"BEGIN TRANSACTION;ALTER TABLE {Mapping.TableMapping.TableName} ADD COLUMN {ToSql()};COMMIT;");
        await pushQuery.ExecuteAsync().ConfigureAwait(false);
    }

    internal string ToSql()
    {
        return $"{Name.Fnuttify()} {Type}";
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToSql();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SqlColumn col
               && string.Equals(Name, col.Name, OrdinalIgnoreCase)
               && Type == col.Type;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (Name.ToUpperInvariant(), Type).GetHashCode();
    }
}
