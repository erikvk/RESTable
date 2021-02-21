using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Tests
{
    public class RequestMaker
    {
        private Client MockClient => Client.External
        (
            clientIp: IPAddress.Parse("151.10.10.5"),
            proxyIp: null,
            userAgent: "Some User-Agent!",
            host: "the host header",
            https: true,
            cookies: new Cookies()
        );

        public async Task<HttpStatusCode> MakeRequest(Method method, string uri, object body, Headers headers)
        {
            var client = MockClient;
            var context = new MockContext(client);
            await using var request = context.CreateRequest(method, uri, body, headers);
            var result = await request.Evaluate().ConfigureAwait(false);
            await using var serialized = await result.Serialize().ConfigureAwait(false);
            return serialized.Result.StatusCode;
        }
    }
}