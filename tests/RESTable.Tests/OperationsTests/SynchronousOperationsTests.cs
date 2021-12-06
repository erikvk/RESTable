using System.Linq;
using System.Threading.Tasks;
using RESTable.Results;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Tests.OperationsTests;

public class SynchronousOperationsTests : OperationsTestBase<ResourceSync>
{
    public SynchronousOperationsTests(RESTableFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SelectCallsSelector()
    {
        Assert.False(OperationsTestsFlags.SelectorWasCalled);
        _ = await Resource.SelectAsync(Request).ToListAsync();
        Assert.True(OperationsTestsFlags.SelectorWasCalled);
    }

    [Fact]
    public async Task InsertCallsInserter()
    {
        Assert.False(OperationsTestsFlags.InserterWasCalled);
        _ = await Resource.InsertAsync(Request).CountAsync();
        Assert.True(OperationsTestsFlags.InserterWasCalled);
    }

    [Fact]
    public async Task UpdatetCallsUpdater()
    {
        Assert.False(OperationsTestsFlags.UpdaterWasCalled);
        _ = await Resource.UpdateAsync(Request).CountAsync();
        Assert.True(OperationsTestsFlags.UpdaterWasCalled);
    }

    [Fact]
    public async Task DeleteCallsDeleter()
    {
        Assert.False(OperationsTestsFlags.DeleterWasCalled);
        _ = await Resource.DeleteAsync(Request);
        Assert.True(OperationsTestsFlags.DeleterWasCalled);
    }

    [Fact]
    public async Task CountCallsCounter()
    {
        Assert.False(OperationsTestsFlags.CounterWasCalled);
        _ = await Resource.CountAsync(Request);
        Assert.True(OperationsTestsFlags.CounterWasCalled);
    }

    [Fact]
    public async Task ValidateCallsValidator()
    {
        Assert.False(OperationsTestsFlags.ValidatorWasCalled);
        var items = Resource.SelectAsync(Request);
        _ = await Resource.Validate(items, Request.Context).ToListAsync();
        Assert.True(OperationsTestsFlags.ValidatorWasCalled);

        var items2List = await Resource.SelectAsync(Request).ToListAsync();
        items2List[5].Id = 99;
        var items2 = items2List.ToAsyncEnumerable();

        await Assert.ThrowsAsync<InvalidInputEntity>(async () =>
        {
            _ = await Resource.Validate(items2, Request.Context).ToListAsync();
        });
    }

    [Fact]
    public async Task AuthenticateCallsAuthenticator()
    {
        Assert.False(OperationsTestsFlags.AuthenticatorWasCalled);
        var success = await Resource.AuthenticateAsync(Request);
        Assert.True(OperationsTestsFlags.AuthenticatorWasCalled);
        Assert.True(success.Success);

        Request.Headers["FailMe"] = "yes";
        var fail = await Resource.AuthenticateAsync(Request);
        Assert.False(fail.Success);
    }
}