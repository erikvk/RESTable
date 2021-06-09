using RESTable.Requests;
using Xunit;

namespace RESTable.Tests.RequestTests
{
    /// <summary>
    /// Helper for making requests
    /// </summary>
    public class RequestTestBase : IClassFixture<RESTableFixture>
    {
        private RESTableFixture Fixture { get; }
        public RESTableContext Context { get; }

        public RequestTestBase(RESTableFixture fixture)
        {
            fixture.Configure();
            Fixture = fixture;
            Context = fixture.Context;
        }
    }
}