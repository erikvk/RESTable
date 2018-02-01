using System;
using System.Collections.Generic;

namespace RESTar
{
    internal class MethodComparer : Comparer<Methods>
    {
        internal static readonly MethodComparer Instance = new MethodComparer();

        public override int Compare(Methods a, Methods b)
        {
            var indexA = Array.IndexOf(RESTarConfig.Methods, a);
            var indexB = Array.IndexOf(RESTarConfig.Methods, b);
            return indexA < indexB ? -1 : (indexB < indexA ? 1 : 0);
        }
    }
}