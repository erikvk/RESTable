using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests;

public class ConverterTests : RESTableTestBase
{
    public ConverterTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.AddGenericJsonConverter(typeof(GenericConverter<>));
        fixture.Configure();
        JsonProvider = fixture.GetRequiredService<IJsonProvider>();
    }

    private IJsonProvider JsonProvider { get; }

    [Fact]
    public void GenericConvertersAreAddedAndUsed()
    {
        var obj = new ToConvert1();

        var serialized = JsonProvider.Serialize(obj);
        var element = JsonProvider.Deserialize<JsonElement>(serialized);

        var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
        Assert.NotNull(genericConverterAddedProperty.Name);

        Assert.Equal(nameof(ToConvert1), genericConverterAddedProperty.Value.GetString());
    }

    [Fact]
    public void GenericConvertersAreAddedAndUsedGenerically()
    {
        // Make sure that multiple types that can be converted by a generic converter, are given their own generic instances 
        // of the generic converter.
        {
            var obj = new ToConvert1();

            var serialized = JsonProvider.Serialize(obj);
            var element = JsonProvider.Deserialize<JsonElement>(serialized);

            var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
            Assert.NotNull(genericConverterAddedProperty.Name);

            Assert.Equal(nameof(ToConvert1), genericConverterAddedProperty.Value.GetString());
        }
        {
            var obj = new ToConvert2();

            var serialized = JsonProvider.Serialize(obj);
            var element = JsonProvider.Deserialize<JsonElement>(serialized);

            var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
            Assert.NotNull(genericConverterAddedProperty.Name);

            Assert.Equal(nameof(ToConvert2), genericConverterAddedProperty.Value.GetString());
        }
    }

    [Fact]
    public void GenericTypesCanBeConverted()
    {
        // Make sure that multiple types that can be converted by a generic converter, are given their own generic instances 
        // of the generic converter.
        var obj = new ToConvertGeneric<string>();

        var serialized = JsonProvider.Serialize(obj);
        var element = JsonProvider.Deserialize<JsonElement>(serialized);

        var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
        Assert.NotNull(genericConverterAddedProperty.Name);

        var str = genericConverterAddedProperty.Value.GetString();
    }
}
