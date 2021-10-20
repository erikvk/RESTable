using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RESTable.Auth;
using RESTable.Requests;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Tests.ApiKeyAuthenticatorTests
{
    /// <summary>
    /// Helper for making requests
    /// </summary>
    public class ApiKeyRequestTestBase : IClassFixture<RESTableFixture>
    {
        private RESTableFixture Fixture { get; }

        public RESTableContext Context { get; }
        public IRequestAuthenticator ApiKeyAuthenticator { get; }
        public string ApiKey => "mysecureapikey";

        public ApiKeyRequestTestBase(RESTableFixture fixture)
        {
            Fixture = fixture;

            var config = new Dictionary<string, object>
            {
                ["RESTable.ApiKeys"] = new ApiKeys
                {
                    new()
                    {
                        ApiKey = ApiKey,
                        AllowAccess = new AllowAccess[]
                        {
                            new()
                            {
                                Resources = new[] {"RESTable.Tests.*"},
                                Methods = new[] {"*"}
                            }
                        }
                    }
                }
            };

            var configJson = JsonConvert.SerializeObject(config);
            using var configJsonStream = new MemoryStream(Encoding.ASCII.GetBytes(configJson));
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(configJsonStream)
                .Build();
            fixture.AddSingleton<IConfiguration>(configuration);
            fixture.AddApiKeys(configuration);

            fixture.Configure();

            Context = fixture.Context;
            ApiKeyAuthenticator = fixture.GetRequiredService<IRequestAuthenticator>();
            Assert.IsType<ApiKeyAuthenticator>(ApiKeyAuthenticator);
        }
    }
}