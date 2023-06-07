using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests;

public class TypeTests : RESTableTestBase
{
    public TypeTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        JsonProvider = fixture.GetRequiredService<IJsonProvider>();
    }

    private IJsonProvider JsonProvider { get; }

    [Fact]
    public void AllTypesSerializeAndDeserialize()
    {
        var testClass = TypeTestsClass.CreatePopulatedInstance(100);
        Assert.Equal(100, testClass.Int);
        var json = JsonProvider.Serialize(testClass);
        Assert.NotEmpty(json);
        var testClass2 = JsonProvider.Deserialize<TypeTestsClass>(json);
        Assert.Equal(testClass, testClass2);
    }

    [Fact]
    public void EnumsAreWrittenAsStrings()
    {
        var testObject = new
        {
            Enum = EnumType.B
        };
        var serializedEnum = JsonProvider.Serialize(testObject, false);

        Assert.Equal("{\"Enum\":\"B\"}", serializedEnum);
    }

    [Fact]
    public void TypesAreWrittenAsStrings()
    {
        var testObject = new
        {
            Type = typeof(string)
        };
        var serializedEnum = JsonProvider.Serialize(testObject, false);

        Assert.Equal("{\"Type\":\"System.String\"}", serializedEnum);
    }
}
