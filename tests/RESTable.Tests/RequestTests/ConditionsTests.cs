using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Tests.RequestTests;

public class ConditionsTests : RESTableTestBase
{
    public ConditionsTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        Context = fixture.Context;
    }

    private RESTableContext Context { get; }

    #region Declared members

    [Fact]
    public async Task NoConditionsNoFilteringDeclared()
    {
        // Base case, a request for 20 things should return 20 things
        await using var request = Context.CreateRequest<TestResource>();
        request.Selector = () => TestResource.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(20, serialized.EntityCount);
    }

    [Fact]
    public async Task OneConditionFiltersDeclared()
    {
        // One condition should filter the results
        await using var request = Context.CreateRequest<TestResource>()
            .WithAddedCondition(nameof(TestResource.Id), Operators.GREATER_THAN, 10);
        request.Selector = () => TestResource.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(10, serialized.EntityCount);
    }

    [Fact]
    public async Task OneParsedConditionFiltersDeclared()
    {
        // The same as above, but from a parsed URI
        await using var request = (IRequest<TestResource>) Context.CreateRequest(uri: "/TestResource/id>10");
        request.Selector = () => TestResource.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(10, serialized.EntityCount);
    }

    [Fact]
    public async Task TwoConditionsFilterDeclared()
    {
        // Two conditions should filter the results
        await using var request = Context.CreateRequest<TestResource>()
            .WithAddedCondition(nameof(TestResource.Id), Operators.GREATER_THAN, 10)
            .WithAddedCondition(nameof(TestResource.Name) + ".1", Operators.EQUALS, 'a'); // second letter in name is 'a'
        request.Selector = () => TestResource.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(5, serialized.EntityCount);
    }

    [Fact]
    public async Task TwoParsedConditionsFilterDeclared()
    {
        // The same as above, but from a parsed URI
        await using var request = (IRequest<TestResource>) Context.CreateRequest(uri: "/TestResource/id>10&name.1=a");
        request.Selector = () => TestResource.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(5, serialized.EntityCount);
    }

    #endregion

    #region Dynamic members

    [Fact]
    public async Task NoConditionsNoFilteringDynamic()
    {
        // Base case, a request for 20 things should return 20 things
        await using var request = Context.CreateRequest<TestResourceDynamic>();
        request.Selector = () => TestResourceDynamic.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(20, serialized.EntityCount);
    }

    [Fact]
    public async Task OneConditionFiltersDynamic()
    {
        // One condition should filter the results
        await using var request = Context.CreateRequest<TestResourceDynamic>()
            .WithAddedCondition("Id", Operators.GREATER_THAN, 10);
        request.Selector = () => TestResourceDynamic.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(10, serialized.EntityCount);
    }

    [Fact]
    public async Task OneParsedConditionFiltersDynamic()
    {
        // The same as above, but from a parsed URI
        await using var request = (IRequest<TestResourceDynamic>) Context.CreateRequest(uri: "/TestResourceDynamic/id>10");
        request.Selector = () => TestResourceDynamic.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(10, serialized.EntityCount);
    }

    [Fact]
    public async Task TwoConditionsFilterDynamic()
    {
        // Two conditions should filter the results
        await using var request = Context.CreateRequest<TestResourceDynamic>()
            .WithAddedCondition("Id", Operators.GREATER_THAN, 10)
            .WithAddedCondition("Name" + ".1", Operators.EQUALS, 'a'); // second letter in name is 'a'
        request.Selector = () => TestResourceDynamic.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(5, serialized.EntityCount);
    }

    [Fact]
    public async Task TwoParsedConditionsFilterDynamic()
    {
        // The same as above, but from a parsed URI
        await using var request = (IRequest<TestResourceDynamic>) Context.CreateRequest(uri: "/TestResourceDynamic/id>10&name.1=a");
        request.Selector = () => TestResourceDynamic.Generate(20);
        await using var result = await request.GetResult();
        await using var serialized = await result.Serialize();
        Assert.Equal(5, serialized.EntityCount);
    }

    #endregion
}