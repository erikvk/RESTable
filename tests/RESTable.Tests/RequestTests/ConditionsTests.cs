using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Tests.OperationsTests;
using Xunit;

namespace RESTable.Tests.RequestTests
{
    public class ConditionsTests : RequestTestBase
    {
        [Fact]
        public async Task NoConditionsNoFiltering()
        {
            // Base case, a request for 20 things should return 20 things
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(20, serialized.EntityCount);
        }

        [Fact]
        public async Task OneConditionFilters()
        {
            // One condition should filter the results
            await using var request = Context.CreateRequest<TestResource>()
                .WithCondition(nameof(TestResource.Id), Operators.GREATER_THAN, 10);
            request.Selector = () => TestResource.Generate(20);
            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(10, serialized.EntityCount);
        }

        [Fact]
        public async Task OneParsedConditionFilters()
        {
            // The same as above, but from a parsed URI
            await using var request = (IRequest<TestResource>) Context.CreateRequest(uri: "/TestResource/id>10");
            request.Selector = () => TestResource.Generate(20);
            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(10, serialized.EntityCount);
        }

        [Fact]
        public async Task TwoConditionsFilter()
        {
            // Two conditions should filter the results
            await using var request = Context.CreateRequest<TestResource>()
                .WithCondition(nameof(TestResource.Id), Operators.GREATER_THAN, 10)
                .WithCondition(nameof(TestResource.Name) + ".1", Operators.EQUALS, 'a'); // second letter in name is 'a'
            request.Selector = () => TestResource.Generate(20);
            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(5, serialized.EntityCount);
        }

        [Fact]
        public async Task TwoParsedConditionsFilter()
        {
            // The same as above, but from a parsed URI
            await using var request = (IRequest<TestResource>) Context.CreateRequest(uri: "/TestResource/id>10&name.1=a");
            request.Selector = () => TestResource.Generate(20);
            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(5, serialized.EntityCount);
        }

        public ConditionsTests(RESTableFixture fixture) : base(fixture) { }
    }
}