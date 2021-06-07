using Xunit;

namespace RESTable.Tests
{
    public class RESTableTestBase : IClassFixture<RESTableFixture>
    {
        protected RESTableFixture Fixture { get; }

        public RESTableTestBase(RESTableFixture fixture)
        {
            Fixture = fixture;
        }
    }
}