// (c) Vasian Cepa 2005
// Version 2

using System.Collections.Generic;

// required for NumericComparer : IComparer only

namespace RESTar.Sorting
{
    public class NumericComparer : IComparer<string>
    {
        public int Compare(string x, string y) => StringLogicalComparer.Compare(x, y);
    }
}