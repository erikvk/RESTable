using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Resources;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests;

[RESTableIgnoreMembersWithType]
public interface IMyInterfaceIgnored
{
    string Str { get; }
}

public interface IMyInterfaceNotIgnored
{
    string Str { get; }
}

public class Implementation : IMyInterfaceIgnored, IMyInterfaceNotIgnored
{
    public string Str => "Foo";
}

public class TestClassIgnored
{
    public string Str { get; set; }
    public IMyInterfaceIgnored MyMember { get; set; } // Should be ignored
}

public class TestClassNotIgnored
{
    public string Str { get; set; }
    public IMyInterfaceNotIgnored MyMember { get; set; } // Should be ignored
}

public class IgnoreTests : RESTableTestBase
{
    public IgnoreTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        JsonProvider = fixture.GetRequiredService<IJsonProvider>();
    }

    private IJsonProvider JsonProvider { get; }

    [Fact]
    public void PropertiesWithIgnoredTypesAreSkippedDuringSerialization()
    {
        var instance = new TestClassIgnored
        {
            Str = "Some string",
            MyMember = new Implementation()
        };

        var json = JsonProvider.Serialize(instance);

        var jsonElement = JsonProvider.Deserialize<JsonElement>(json);

        Assert.Single(jsonElement.EnumerateObject());
        var singleProperty = jsonElement.EnumerateObject().Single();
        Assert.Equal("Str", singleProperty.Name);
        Assert.Equal(instance.Str, singleProperty.Value.GetString());
    }


    [Fact]
    public void PropertiesWithIgnoredTypesAreSkippedDuringDeserialization()
    {
        var instance = new TestClassNotIgnored
        {
            Str = "Some string",
            MyMember = new Implementation()
        };

        var json = JsonProvider.Serialize(instance);
        var jsonElement = JsonProvider.Deserialize<JsonElement>(json);

        // Ensure MyMember is present in the deserialized object
        Assert.Equal(2, jsonElement.EnumerateObject().Count());
        var firstProperty = jsonElement.EnumerateObject().First();
        var secondProperty = jsonElement.EnumerateObject().Last();
        Assert.Equal("Str", firstProperty.Name);
        Assert.Equal(instance.Str, firstProperty.Value.GetString());
        Assert.Equal("MyMember", secondProperty.Name);
        Assert.Equal("Foo", secondProperty.Value.EnumerateObject().First().Value.GetString());

        // Ensure MyMember is ignored when deserializing to the class with the ignored member
        var deserialized = JsonProvider.Deserialize<TestClassIgnored>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(instance.Str, deserialized.Str);
        Assert.Null(deserialized.MyMember);
    }
}
