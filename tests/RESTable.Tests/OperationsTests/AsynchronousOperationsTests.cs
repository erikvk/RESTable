﻿using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RESTable.Tests.OperationsTests
{
    public class AsynchronousOperationsTests : OperationsTestBase<ResourceAsync>
    {
        [Fact]
        public async Task SelectCallsAsyncSelector()
        {
            Assert.False(OperationsTestsFlags.AsyncSelectorWasCalled);
            _ = await Resource.SelectAsync(Request).ToListAsync();
            Assert.True(OperationsTestsFlags.AsyncSelectorWasCalled);
        }

        [Fact]
        public async Task InsertCallsAsyncInserter()
        {
            Assert.False(OperationsTestsFlags.AsyncInserterWasCalled);
            _ = await Resource.InsertAsync(Request);
            Assert.True(OperationsTestsFlags.AsyncInserterWasCalled);
        }

        [Fact]
        public async Task UpdatetCallsAsyncUpdater()
        {
            Assert.False(OperationsTestsFlags.AsyncUpdaterWasCalled);
            _ = await Resource.UpdateAsync(Request);
            Assert.True(OperationsTestsFlags.AsyncUpdaterWasCalled);
        }

        [Fact]
        public async Task DeleteCallsAsyncDeleter()
        {
            Assert.False(OperationsTestsFlags.AsyncDeleterWasCalled);
            _ = await Resource.DeleteAsync(Request);
            Assert.True(OperationsTestsFlags.AsyncDeleterWasCalled);
        }

        [Fact]
        public async Task CountCallsAsyncCounter()
        {
            Assert.False(OperationsTestsFlags.AsyncCounterWasCalled);
            _ = await Resource.CountAsync(Request);
            Assert.True(OperationsTestsFlags.AsyncCounterWasCalled);
        }

        [Fact]
        public async Task AuthenticateCallsAsyncAuthenticator()
        {
            Assert.False(OperationsTestsFlags.AsyncAuthenticatorWasCalled);
            var success = await Resource.AuthenticateAsync(Request);
            Assert.True(OperationsTestsFlags.AsyncAuthenticatorWasCalled);
            Assert.True(success.Success);

            Request.Headers["FailMe"] = "yes";
            var fail = await Resource.AuthenticateAsync(Request);
            Assert.False(fail.Success);
        }

        public AsynchronousOperationsTests(RESTableFixture fixture) : base(fixture) { }
    }
}