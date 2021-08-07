using System;
using System.Collections.Generic;

namespace RESTable
{
    public class DynamicMemberPopulatorCacheEqualityComparer : IEqualityComparer<(string, Type)>
    {
        public static readonly IEqualityComparer<(string, Type)> Instance = new DynamicMemberPopulatorCacheEqualityComparer();
        public bool Equals((string, Type) x, (string, Type) y) => x.Item2 == y.Item2 && string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string, Type) obj) => (obj.Item1.ToLowerInvariant(), obj.Item2).GetHashCode();
    }
}