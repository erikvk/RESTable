using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests;

public class DynamicJsonReaderTests : RESTableTestBase
{
    public DynamicJsonReaderTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        JsonProvider = fixture.GetRequiredService<IJsonProvider>();
    }

    private IJsonProvider JsonProvider { get; }

    [Fact]
    public void TryReadNextPropertyTest()
    {
        var simpleHolder = new Holder<string, string>
        {
            First = "First thing",
            Second = "Second thing"
        };

        var json = JsonProvider.SerializeToUtf8Bytes(simpleHolder);

        var reader = new Utf8JsonReader(json);
        var jsonReader = JsonProvider.GetJsonReader();

        var foundFirst = jsonReader.TryReadNextProperty(ref reader, typeof(string), out var name, out var value);

        Assert.True(foundFirst);
        Assert.IsType<string>(value);
        Assert.Equal("First", name);
        Assert.Equal(simpleHolder.First, value);

        var foundSecond = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.True(foundSecond);
        Assert.IsType<string>(value);
        Assert.Equal("Second", name);
        Assert.Equal(simpleHolder.Second, value);

        var foundThird = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.False(foundThird);
        Assert.Null(name);
        Assert.Null(value);
    }

    [Fact]
    public void CombineReadPropertyWithReadToObjectOrDefaultTest()
    {
        var holder = new Holder<string, string, string, string>
        {
            First = "First thing",
            Second = "Second thing",
            Third = "Third thing",
            Fourth = "Fourth thing"
        };

        var json = JsonProvider.SerializeToUtf8Bytes(holder);

        var reader = new Utf8JsonReader(json);
        var jsonReader = JsonProvider.GetJsonReader();

        var foundFirst = jsonReader.TryReadNextProperty(ref reader, typeof(string), out var name, out var value);

        Assert.True(foundFirst);
        Assert.IsType<string>(value);
        Assert.Equal("First", name);
        Assert.Equal(holder.First, value);

        ISerializationMetadata holderMetadata = Fixture.GetRequiredService<ISerializationMetadata<Holder<string, string, string, string>>>();

        var instance = jsonReader.ReadToObjectOrGetDefault(ref reader, holderMetadata);

        Assert.NotNull(instance);
        Assert.IsType<Holder<string, string, string, string>>(instance);

        var castInstance = (Holder<string, string, string, string>) instance;

        Assert.Null(castInstance.First);
        Assert.Equal(holder.Second, castInstance.Second);
        Assert.Equal(holder.Third, castInstance.Third);
        Assert.Equal(holder.Fourth, castInstance.Fourth);

        var foundAdditional = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.False(foundAdditional);
        Assert.Null(name);
        Assert.Null(value);
    }

    [Fact]
    public void CombineReadPropertyWithReadToObjectTest()
    {
        var holder = new Holder<string, string, string, string>
        {
            First = "First thing",
            Second = "Second thing",
            Third = "Third thing",
            Fourth = "Fourth thing"
        };

        var json = JsonProvider.SerializeToUtf8Bytes(holder);

        var reader = new Utf8JsonReader(json);
        var jsonReader = JsonProvider.GetJsonReader();

        var foundFirst = jsonReader.TryReadNextProperty(ref reader, typeof(string), out var name, out var value);

        Assert.True(foundFirst);
        Assert.IsType<string>(value);
        Assert.Equal("First", name);
        Assert.Equal(holder.First, value);

        ISerializationMetadata holderMetadata = Fixture.GetRequiredService<ISerializationMetadata<Holder<string, string, string, string>>>();

        var instance = jsonReader.ReadToObject(ref reader, holderMetadata);
        Assert.IsType<Holder<string, string, string, string>>(instance);

        var castInstance = (Holder<string, string, string, string>) instance;

        Assert.NotNull(instance);
        Assert.Null(castInstance.First);
        Assert.Equal(holder.Second, castInstance.Second);
        Assert.Equal(holder.Third, castInstance.Third);
        Assert.Equal(holder.Fourth, castInstance.Fourth);

        var foundAdditional = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.False(foundAdditional);
        Assert.Null(name);
        Assert.Null(value);
    }

    [Fact]
    public void ReadingEmptyObjectTest()
    {
        var empty = new { };

        var json = JsonProvider.SerializeToUtf8Bytes(empty);

        var reader = new Utf8JsonReader(json);
        var jsonReader = JsonProvider.GetJsonReader();

        var foundFirst = jsonReader.TryReadNextProperty(ref reader, typeof(string), out var name, out var value);

        Assert.False(foundFirst);
        Assert.Null(name);
        Assert.Null(value);

        var foundSecond = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.False(foundSecond);
        Assert.Null(name);
        Assert.Null(value);

        var foundThird = jsonReader.TryReadNextProperty(ref reader, typeof(string), out name, out value);

        Assert.False(foundThird);
        Assert.Null(name);
        Assert.Null(value);
    }

    [Fact]
    public void TryReadNextPropertyWithDifferentTypesTest()
    {
        var complexHolder = new Holder<string, Holder<DateTime, List<int>>>
        {
            First = "First thing",
            Second = new Holder<DateTime, List<int>>
            {
                First = DateTime.UtcNow,
                Second = new List<int> { 1, 2, 3 }
            }
        };

        var json = JsonProvider.SerializeToUtf8Bytes(complexHolder);

        var reader = new Utf8JsonReader(json);
        var jsonReader = JsonProvider.GetJsonReader();

        var foundFirst = jsonReader.TryReadNextProperty(ref reader, typeof(string), out var name, out var stringValue);

        Assert.True(foundFirst);
        Assert.IsType<string>(stringValue);
        Assert.Equal("First", name);
        Assert.Equal(complexHolder.First, stringValue);

        var foundSecond = jsonReader.TryReadNextProperty(ref reader, typeof(Holder<DateTime, List<int>>), out name, out var complexValue);

        Assert.True(foundSecond);
        Assert.IsType<Holder<DateTime, List<int>>>(complexValue);
        var castComplexValue = (Holder<DateTime, List<int>>) complexValue;
        Assert.Equal("Second", name);
        Assert.Equal(complexHolder.Second.First, castComplexValue.First);
        Assert.True(complexHolder.Second.Second.SequenceEqual(castComplexValue.Second));

        var foundThird = jsonReader.TryReadNextProperty(ref reader, typeof(object), out name, out var anyValue);

        Assert.False(foundThird);
        Assert.Null(name);
        Assert.Null(anyValue);
    }
}
