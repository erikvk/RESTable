using RESTable.Requests;
using Xunit;

namespace RESTable.Tests.ApiKeyAuthenticatorTests
{
    public class ApiKeyRequestTests : ApiKeyRequestTestBase
    {
        [Fact]
        public void ApiKeyIsRequiredInRequests()
        {
            var uri = "/testresource";
            var headers = new Headers {Authorization = "apikey notAnApiKey"};

            var authSuccess = ApiKeyAuthenticator.TryAuthenticate(ref uri, headers, out var accessRights);

            Assert.False(authSuccess);
            Assert.Empty(accessRights);
        }

        [Fact]
        public void ApiKeyInHeaderWorks()
        {
            var uri = "/testresource";
            var headers = new Headers {Authorization = $"apikey {ApiKey}"};
            var authSuccess = ApiKeyAuthenticator.TryAuthenticate(ref uri, headers, out var accessRights);

            Assert.True(authSuccess);
            Assert.NotEmpty(accessRights);
        }

        [Fact]
        public void ApiKeyInUriWorks()
        {
            var uri = $"({ApiKey})/testresource";
            var authSuccess = ApiKeyAuthenticator.TryAuthenticate(ref uri, null, out var accessRights);

            Assert.True(authSuccess);
            Assert.NotEmpty(accessRights);
        }

        public ApiKeyRequestTests(RESTableFixture fixture) : base(fixture) { }
    }
}