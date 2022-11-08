using System;
using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Internal;

internal class MethodComparer : Comparer<Method>
{
    internal static readonly MethodComparer Instance = new();

    public override int Compare(Method a, Method b)
    {
        var methods = EnumMember<Method>.Values;
        var indexA = Array.IndexOf(methods, a);
        var indexB = Array.IndexOf(methods, b);
        return indexA < indexB ? -1 : indexB < indexA ? 1 : 0;
    }
}
