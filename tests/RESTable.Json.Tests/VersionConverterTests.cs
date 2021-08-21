using System;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Tests;
using Xunit;

namespace RESTable.Json.Tests
{
    public class VersionConverterTests : RESTableTestBase
    {
        [Fact]
        public void ReadVersionFromString()
        {
            var jsonProvider = Fixture.GetRequiredService<IJsonProvider>();
            var versionString = "12.1.2.3";
            var version = new Version(versionString);
            var jsonString = $"{{\"Item\":\"{versionString}\"}}";
            var versionHolder = jsonProvider.Deserialize<Holder<Version>>(jsonString);
            Assert.NotNull(versionHolder?.Item);
            Assert.Equal(version, versionHolder.Item);
        }

        [Fact]
        public void WriteVersionToString()
        {
            var jsonProvider = Fixture.GetRequiredService<IJsonProvider>();
            var versionString = "12.1.2.3";
            var version = new Version(versionString);
            var jsonString = $"{{\"Item\":\"{versionString}\"}}";
            var versionHolder = new Holder<Version> {Item = version};
            var str = jsonProvider.Serialize(versionHolder, false);
            Assert.Equal(jsonString, str);
        }

        public VersionConverterTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.AddJson();
            fixture.Configure();
        }
    }
}