// (c) Vasian Cepa 2005
// Version 2

using System.Collections.Generic;

// required for NumericComparer : IComparer only

namespace RESTar
{
    // Original version:

//	public class NumericComparer : IComparer
//	{
//		public NumericComparer()
//		{}
//		
//		public int Compare(object x, object y)
//		{
//			if((x is string) && (y is string))
//			{
//				return StringLogicalComparer.Compare((string)x, (string)y);
//			}
//			return -1;
//		}
//	}//EOC

    // Modified version by Erik von Krusenstierna to work with only strings:

    public class RESTarComparer : IComparer<object>
    {
        public int Compare(object x, object y) => x is string xs && y is string ys
            ? StringLogicalComparer.Compare(xs, ys)
            : Comparer<object>.Default.Compare(x, y);
    }
}