using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Tests;

namespace RESTable.Json.Tests
{
    public class SerializationTests : RESTableTestBase
    {
        private IJsonProvider JsonProvider { get; }

        public SerializationTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.AddJson();
            fixture.Configure();
            JsonProvider = fixture.GetRequiredService<IJsonProvider>();
        }
    }
}