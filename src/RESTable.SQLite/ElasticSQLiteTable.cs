using System;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.Sqlite;

/// <inheritdoc cref="SqliteTable" />
/// <inheritdoc cref="IDynamicMemberValueProvider" />
/// <summary>
///     Defines an elastic Sqlite table
/// </summary>
public abstract class ElasticSqliteTable : SqliteTable, IDynamicMemberValueProvider
{
    /// <summary>
    ///     Creates a new instance of this ElasticSqliteTable type
    /// </summary>
    protected ElasticSqliteTable()
    {
        var tableMapping = TableMapping.GetTableMapping(GetType());
        if (tableMapping is null)
            throw new InvalidOperationException($"No table mapping for type '{GetType().GetRESTableTypeName()}'");
        DynamicMembers = new DynamicMemberCollection(tableMapping);
    }

    /// <summary>
    ///     The dynamic members stored for this instance
    /// </summary>
    [SqliteMember(true)]
    [RESTableMember(hide: true, mergeOntoOwner: true)]
    public DynamicMemberCollection DynamicMembers { get; }

    /// <summary>
    ///     Indexer used for access to dynamic members
    /// </summary>
    public object? this[string memberName]
    {
        get => DynamicMembers.SafeGet(memberName);
        set => DynamicMembers.TrySetValue(memberName, value);
    }

    /// <inheritdoc />
    public bool TryGetValue(string memberName, out object? value, out string? actualMemberName)
    {
        return DynamicMembers.TryGetValue(memberName, out value, out actualMemberName);
    }

    /// <inheritdoc />
    public bool TrySetValue(string memberName, object? value)
    {
        return DynamicMembers.TrySetValue(memberName, value);
    }
}
