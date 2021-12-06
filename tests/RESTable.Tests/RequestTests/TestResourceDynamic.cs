using System;
using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

#pragma warning disable 1998

namespace RESTable.Tests.RequestTests;

[RESTable(AllowDynamicConditions = true)]
public class TestResourceDynamic :
    Dictionary<string, object?>,
    ISelector<TestResourceDynamic>,
    IInserter<TestResourceDynamic>,
    IUpdater<TestResourceDynamic>,
    IDeleter<TestResourceDynamic>
{
    public TestResourceDynamic(int id, string name)
    {
        this["Id"] = id;
        this["Name"] = name + id;
    }

    public int Delete(IRequest<TestResourceDynamic> request)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<TestResourceDynamic> Insert(IRequest<TestResourceDynamic> request)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<TestResourceDynamic> Select(IRequest<TestResourceDynamic> request)
    {
        // We use the request's Selector delegates instead of this
        throw new NotImplementedException();
    }

    public IEnumerable<TestResourceDynamic> Update(IRequest<TestResourceDynamic> request)
    {
        throw new NotImplementedException();
    }

    public static async IAsyncEnumerable<TestResourceDynamic> Generate(int number)
    {
        for (var i = 1; i <= number; i += 1)
        {
            var name = i % 2 == 0 ? "John " : "Jane ";
            yield return new TestResourceDynamic(i, name);
        }
    }
}