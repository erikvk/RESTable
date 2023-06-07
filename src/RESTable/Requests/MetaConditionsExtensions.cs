using System;

namespace RESTable.Requests;

internal static class MetaConditionsExtensions
{
    internal static Type GetExpectedType(this RESTableMetaCondition condition)
    {
        return condition switch
        {
            RESTableMetaCondition.Unsafe => typeof(bool),
            RESTableMetaCondition.Limit => typeof(int),
            RESTableMetaCondition.Offset => typeof(int),
            RESTableMetaCondition.Order_asc => typeof(string),
            RESTableMetaCondition.Order_desc => typeof(string),
            RESTableMetaCondition.Select => typeof(string),
            RESTableMetaCondition.Add => typeof(string),
            RESTableMetaCondition.Rename => typeof(string),
            RESTableMetaCondition.Distinct => typeof(bool),
            RESTableMetaCondition.Search => typeof(string),
            RESTableMetaCondition.Search_regex => typeof(string),
            RESTableMetaCondition.Safepost => typeof(string),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null)
        };
    }
}
