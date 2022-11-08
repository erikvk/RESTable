using System;

namespace RESTable.Sqlite;

/// <inheritdoc />
/// <summary>
///     Configure how this member is bound to an SQLite table column. Can only be
///     used on public instance auto properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SqliteMemberAttribute : Attribute
{
    /// <inheritdoc />
    /// <summary>
    ///     Creates a new instance of the <see cref="SqliteMemberAttribute" /> class.
    /// </summary>
    /// <param name="ignore">
    ///     Should this property be ignored by RESTable.SQLite? Does not imply that the property
    ///     is ignored by RESTable, merely that it is not mapped to an SQLite table column.
    /// </param>
    /// <param name="columnName">
    ///     The name of the column to map this property with. If null, the property name
    ///     is used.
    /// </param>
    public SqliteMemberAttribute(bool ignore = false, string? columnName = null)
    {
        Ignored = ignore;
        ColumnName = columnName;
    }

    /// <summary>
    ///     Is this property ignored by RESTable.SQLite? Does not imply that the property
    ///     is ignored by RESTable, merely that it is not mapped to an SQLite table column.
    /// </summary>
    public bool Ignored { get; }

    /// <summary>
    ///     The name of the column to map this property with. If null, the property name
    ///     is used.
    /// </summary>
    public string? ColumnName { get; }
}
