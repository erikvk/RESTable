using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTable.Sqlite.Meta;

internal class NoCaseComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
    }

    public int GetHashCode(string obj)
    {
        return obj.ToLower().GetHashCode();
    }
}

/// <inheritdoc />
/// <summary>
///     A collection of ColumnMapping instances, indexed on CLR property name
/// </summary>
public class ColumnMappings : Dictionary<string, ColumnMapping>
{
    /// <inheritdoc />
    public ColumnMappings(IEnumerable<ColumnMapping> collection) : base(new NoCaseComparer())
    {
        foreach (var item in collection)
            this[item.ClrProperty.Name] = item;
    }

    internal string ToSql()
    {
        return string.Join(", ", Values.Where(m => !m.IsRowId).Select(c => c.SqlColumn.ToSql()));
    }

    internal async Task Push()
    {
        foreach (var mapping in Values)
            await mapping.Push().ConfigureAwait(false);
    }
}