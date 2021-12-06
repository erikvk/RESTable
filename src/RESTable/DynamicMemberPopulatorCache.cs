using System;
using System.Collections.Generic;

namespace RESTable;

public class DynamicMemberPopulatorCache : Dictionary<(string key, Type type), Populator>
{
    public DynamicMemberPopulatorCache() : base(DynamicMemberPopulatorCacheEqualityComparer.Instance) { }
}