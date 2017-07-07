using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RESTar.Deflection
{
    internal static class SQLCache
    {
        internal static readonly IDictionary<int, string> SQLQueries;
        static SQLCache() => SQLQueries = new ConcurrentDictionary<int, string>();
    }
}
