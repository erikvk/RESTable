using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests
{
    public class ParameterizedConstructorTests : RESTableTestBase
    {
        private IJsonProvider JsonProvider { get; }

        [Fact]
        public void InvokeSimpleParameterizedConstructor()
        {
            var holder = new Holder<string, string> {First = "First", Second = "Second"};
            var json = JsonProvider.SerializeToUtf8Bytes(holder);
            var reader = new Utf8JsonReader(json);
            var jsonReader = JsonProvider.GetJsonReader();

            var parameterizedMetadata = Fixture.GetRequiredService<ISerializationMetadata<ParameterizedHolder<string, string>>>();

            var deserializedHolder = jsonReader.ReadToObject(ref reader, parameterizedMetadata);
            Assert.Equal(holder.First, deserializedHolder.First);
            Assert.Equal(holder.Second, deserializedHolder.Second);
        }

        [Fact]
        public void InvokeComplexParameterizedConstructor()
        {
            var item = new ParameterizedConstructorTestClass("Str", 100, DateTime.Now, new Holder<int> {Item = 200});
            var json = JsonProvider.SerializeToUtf8Bytes(item);
            var reader = new Utf8JsonReader(json);
            var jsonReader = JsonProvider.GetJsonReader();

            var parameterizedMetadata = Fixture.GetRequiredService<ISerializationMetadata<ParameterizedConstructorTestClass>>();

            var deserializedHolder = jsonReader.ReadToObject(ref reader, parameterizedMetadata);
            Assert.Equal(item.Str, deserializedHolder.Str);
            Assert.Equal(item.Int, deserializedHolder.Int);
            Assert.Equal(item.DateTime, deserializedHolder.DateTime);
            Assert.Equal(item.IntHolder.Item, deserializedHolder.IntHolder.Item);
        }

        private void InvokeComplexParameterizedOptionalsConstructorInternal(ParameterizedConstructorOptionalsTestClass item)
        {
            var json = JsonProvider.SerializeToUtf8Bytes(item);
            var reader = new Utf8JsonReader(json);
            var jsonReader = JsonProvider.GetJsonReader();

            var parameterizedMetadata = Fixture.GetRequiredService<ISerializationMetadata<ParameterizedConstructorOptionalsTestClass>>();

            var deserializedHolder = jsonReader.ReadToObject(ref reader, parameterizedMetadata);
            Assert.Equal(item.Str, deserializedHolder.Str);
            Assert.Equal(item.Int, deserializedHolder.Int);
            Assert.Equal(item.DateTime, deserializedHolder.DateTime);
            Assert.Equal(item.IntHolder?.Item, deserializedHolder.IntHolder?.Item);
        }

        [Fact]
        public void InvokeComplexParameterizedOptionalsConstructor()
        {
            var item1 = new ParameterizedConstructorOptionalsTestClass(100, DateTime.Now, new Holder<int> {Item = 200}, "Str");
            InvokeComplexParameterizedOptionalsConstructorInternal(item1);
            var item2 = new ParameterizedConstructorOptionalsTestClass(100, DateTime.Now);
            InvokeComplexParameterizedOptionalsConstructorInternal(item2);
            var item3 = new ParameterizedConstructorOptionalsTestClass(100, DateTime.Now, str: "Foo");
            InvokeComplexParameterizedOptionalsConstructorInternal(item3);
            var item4 = new ParameterizedConstructorOptionalsTestClass(100, DateTime.Now, intHolder: new Holder<int> {Item = 421});
            InvokeComplexParameterizedOptionalsConstructorInternal(item4);
        }

        public ParameterizedConstructorTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.Configure();
            JsonProvider = fixture.GetRequiredService<IJsonProvider>();
        }
    }
}