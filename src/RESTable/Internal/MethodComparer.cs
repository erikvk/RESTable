using System;
using System.Collections.Generic;

namespace RESTable.Internal
{
    internal class MethodComparer : Comparer<Method>
    {
        internal static readonly MethodComparer Instance = new MethodComparer();

        public override int Compare(Method a, Method b)
        {
            var indexA = Array.IndexOf(RESTableConfig.Methods, a);
            var indexB = Array.IndexOf(RESTableConfig.Methods, b);
            return indexA < indexB ? -1 : indexB < indexA ? 1 : 0;
        }
    }
}