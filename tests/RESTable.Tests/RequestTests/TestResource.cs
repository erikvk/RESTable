using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

#pragma warning disable 1998

namespace RESTable.Tests.RequestTests
{
    [RESTable]
    public class TestResource : ISelector<TestResource>, IInserter<TestResource>, IUpdater<TestResource>, IDeleter<TestResource>
    {
        public int Id { get; }
        public string Name { get; }

        public int? FavoriteNumber { get; set; }

        public TestResource(int id, string name)
        {
            Id = id;
            Name = name + id;
        }

        public static async IAsyncEnumerable<TestResource> Generate(int number)
        {
            for (var i = 1; i <= number; i += 1)
            {
                var name = i % 2 == 0 ? "John " : "Jane ";
                yield return new TestResource(i, name);
            }
        }

        public IEnumerable<TestResource> Select(IRequest<TestResource> request)
        {
            // We use the request's Selector delegates instead of this
            throw new System.NotImplementedException();
        }

        public IEnumerable<TestResource> Insert(IRequest<TestResource> request)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<TestResource> Update(IRequest<TestResource> request)
        {
            throw new System.NotImplementedException();
        }

        public int Delete(IRequest<TestResource> request)
        {
            throw new System.NotImplementedException();
        }
    }
}