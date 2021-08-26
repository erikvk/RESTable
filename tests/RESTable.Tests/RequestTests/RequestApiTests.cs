using Microsoft.Extensions.DependencyInjection;

namespace RESTable.Tests.RequestTests
{
    public class RequestApiTests : RESTableTestBase
    {
        public RequestApiTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.AddJson();
            Fixture.Configure();
        }
    }
}