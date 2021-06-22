using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;
using Xunit;

namespace RESTable.Tests.RequestTests
{
    public class RequestApiTests : RequestTestBase
    {
        [Fact]
        public async Task GetResultsShouldDispose()
        {
            var request = Context.CreateRequest<TestResource>().WithSelector(() => TestResource.Generate(100));
            var first = await request.GetResultEntities().FirstAsync();
            var a = "";
        }

        public RequestApiTests(RESTableFixture fixture) : base(fixture) { }
    }
}