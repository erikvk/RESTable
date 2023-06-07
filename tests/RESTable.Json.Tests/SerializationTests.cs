using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Xunit;

namespace RESTable.Json.Tests;

public class SerializationTests : RESTableTestBase
{
    public SerializationTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        JsonProvider = fixture.GetRequiredService<IJsonProvider>();
    }

    private IJsonProvider JsonProvider { get; }
}
