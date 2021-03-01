using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RESTable.Tests
{
    public class SynchronousOperationsTests : OperationsTests<ResourceSync>
    {

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
            _ = await Resource.InsertAsync(Request);
            Assert.True(OperationsTestsFlags.InserterWasCalled);
        }

        [Fact]
        public async Task UpdatetCallsUpdater()
        {
            Assert.False(OperationsTestsFlags.UpdaterWasCalled);
            _ = await Resource.UpdateAsync(Request);
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
        }

        [Fact]
        public async Task AuthenticateCallsAuthenticator()
        {
            Assert.False(OperationsTestsFlags.AuthenticatorWasCalled);
            _ = await Resource.AuthenticateAsync(Request);
            Assert.True(OperationsTestsFlags.AuthenticatorWasCalled);
        }

        public SynchronousOperationsTests(RESTableFixture fixture) : base(fixture) { }
    }
}