using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Tests;
using Xunit;

namespace RESTable.Json.Tests
{
    public class VersionHolder
    {
        public Version Version { get; set; }
    }

    public class VersionConverterTests : RESTableTestBase
    {
        [Fact]
        public async Task ReadVersionFromString()
        {
            var jsonProvider = Fixture.GetRequiredService<IJsonProvider>();
            var versionString = "12.1.2.3";
            var version = new Version(versionString);
            var jsonString = $"{{\"Version\":\"{versionString}\"}}";
            var versionHolder = await jsonProvider.DeserializeAsync<VersionHolder>(jsonString).ConfigureAwait(false);
            Assert.NotNull(versionHolder?.Version);
            Assert.Equal(version, versionHolder.Version);
        }

        [Fact]
        public async Task WriteVersionToString()
        {
            var jsonProvider = Fixture.GetRequiredService<IJsonProvider>();
            var versionString = "12.1.2.3";
            var version = new Version(versionString);
            var jsonString = $"{{\"Version\":\"{versionString}\"}}";
            var versionHolder = new VersionHolder {Version = version};
            var str = await jsonProvider.SerializeAsync((IJsonWriter) versionHolder, (object) false).ConfigureAwait(false);
            Assert.Equal(jsonString, str);
        }

        public VersionConverterTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.Configure();
        }
    }
}