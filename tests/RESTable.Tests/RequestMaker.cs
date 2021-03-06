﻿using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Tests
{
    public class RequestMaker
    {
        private RESTableFixture Fixture { get; }

        public RequestMaker(RESTableFixture fixture)
        {
            Fixture = fixture;
        }

        public async Task<HttpStatusCode> MakeRequest(Method method, string uri, object body, Headers headers)
        {
            var context = new MockContext(Fixture.ServiceProvider);
            await using var request = context.CreateRequest(method, uri, body, headers);
            var result = await request.Evaluate().ConfigureAwait(false);
            await using var serialized = await result.Serialize().ConfigureAwait(false);
            return serialized.Result.StatusCode;
        }
    }
}