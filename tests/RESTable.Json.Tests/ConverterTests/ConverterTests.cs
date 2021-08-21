using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Tests;
using Xunit;

namespace RESTable.Json.Tests
{
    public class ConverterTests : RESTableTestBase
    {
        private IJsonProvider JsonProvider { get; }

        [Fact]
        public void GenericConvertersAreAddedAndUsed()
        {
            var obj = new ConvertMePlease1();

            var serialized = JsonProvider.Serialize(obj);
            var element = JsonProvider.Deserialize<JsonElement>(serialized);

            var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
            Assert.NotNull(genericConverterAddedProperty.Name);

            Assert.Equal(genericConverterAddedProperty.Value.GetString(), typeof(ConvertMePlease1).FullName);
        }

        [Fact]
        public void GenericConvertersAreAddedAndUsedGenerically()
        {
            // Make sure that multiple types that can be converted by a generic converter, are given their own generic instances 
            // of the generic converter.
            {
                var obj = new ConvertMePlease1();

                var serialized = JsonProvider.Serialize(obj);
                var element = JsonProvider.Deserialize<JsonElement>(serialized);

                var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
                Assert.NotNull(genericConverterAddedProperty.Name);

                Assert.Equal(genericConverterAddedProperty.Value.GetString(), typeof(ConvertMePlease1).FullName);
            }
            {
                var obj = new ConvertMePlease2();

                var serialized = JsonProvider.Serialize(obj);
                var element = JsonProvider.Deserialize<JsonElement>(serialized);

                var genericConverterAddedProperty = element.EnumerateObject().FirstOrDefault(p => p.NameEquals("GenericConverter"));
                Assert.NotNull(genericConverterAddedProperty.Name);

                Assert.Equal(genericConverterAddedProperty.Value.GetString(), typeof(ConvertMePlease2).FullName);
            }
        }

        public ConverterTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.AddJson();
            fixture.AddGenericJsonConverter(typeof(GenericConverter<>));
            fixture.Configure();
            JsonProvider = fixture.GetRequiredService<IJsonProvider>();
        }
    }
}