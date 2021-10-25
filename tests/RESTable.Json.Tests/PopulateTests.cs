using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Json.Tests
{
    public class PopulateTests : RESTableTestBase
    {
        private IJsonProvider JsonProvider { get; }

        [Fact]
        public async Task PopulateVersionWithString()
        {
            var version = new Version(21, 1, 1);
            var obj = new Holder<Version>
            {
                Item = version
            };

            var newVersion = new Version(21, 1, 2);
            var newObj = new Holder<Version>
            {
                Item = newVersion
            };
            var newJson = JsonProvider.Serialize(newObj);

            var populator = JsonProvider.GetPopulator<Holder<Version>>(newJson);
            await populator(obj);

            Assert.Equal(obj.Item, newObj.Item);
        }

        public class MyHolder : Holder<string>
        {
            public string OtherItem { get; set; }
        }

        [Fact]
        public async Task PopulateInheritedProperties()
        {
            var instance = new MyHolder
            {
                Item = "Goo",
                OtherItem = "Goo"
            };
            var json = JsonProvider.Serialize(new {Item = "Foo", OtherItem = "Foo"});
            var populator = JsonProvider.GetPopulator<MyHolder>(json);
            var populated = await populator(instance) as MyHolder;
            Assert.NotNull(populated);
            Assert.Equal(instance, populated);
            Assert.Equal("Foo", populated.OtherItem);
            Assert.Equal("Foo", populated.Item);
        }

        public PopulateTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.Configure();
            JsonProvider = fixture.GetRequiredService<IJsonProvider>();
        }
    }
}