using System;

namespace RESTable.Requests
{
    internal static class MetaConditionsExtensions
    {
        internal static Type GetExpectedType(this RESTableMetaCondition condition)
        {
            switch (condition)
            {
                case RESTableMetaCondition.Unsafe: return typeof(bool);
                case RESTableMetaCondition.Limit: return typeof(int);
                case RESTableMetaCondition.Offset: return typeof(int);
                case RESTableMetaCondition.Order_asc: return typeof(string);
                case RESTableMetaCondition.Order_desc: return typeof(string);
                case RESTableMetaCondition.Select: return typeof(string);
                case RESTableMetaCondition.Add: return typeof(string);
                case RESTableMetaCondition.Rename: return typeof(string);
                case RESTableMetaCondition.Distinct: return typeof(bool);
                case RESTableMetaCondition.Search: return typeof(string);
                case RESTableMetaCondition.Search_regex: return typeof(string);
                case RESTableMetaCondition.Safepost: return typeof(string);
                default: throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
        }
    }
}