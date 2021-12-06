using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1998

namespace RESTable.Tests.OperationsTests;

public class ResourceOperationsBase
{
    public int Id { get; set; }
}

public class ResourceOperationsBase<TDerived> : ResourceOperationsBase where TDerived : ResourceOperationsBase<TDerived>, new()
{
    protected static IEnumerable<TDerived> Entities => Enumerable.Range(0, 50).Select(i => new TDerived {Id = i});
    protected static IAsyncEnumerable<TDerived> AsyncEntities => Entities.ToAsyncEnumerable();
}