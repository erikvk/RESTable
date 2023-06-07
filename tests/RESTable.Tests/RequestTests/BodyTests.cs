using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Tests.RequestTests;

public class BodyTests : RESTableTestBase
{
    private const string StringData = "This is the string data that will be used in tests to make sure that " +
                                      "various conversions that is used by Body will not mess things up %1321ÅÄÖ🎆🎆🎁🎞";

    public BodyTests(RESTableFixture fixture) : base(fixture)
    {
        fixture.Configure();
        Context = fixture.Context;
    }

    private RESTableContext Context { get; }

    [Fact]
    public async Task BodyFromVariousBinaryTypes()
    {
        async Task EvaluateAndCompareBody(object? bodyObject)
        {
            await using var protocolHolder = Context.CreateRequest<Echo>();
            var body = bodyObject as Body ?? new Body(protocolHolder, bodyObject);
            await using (body)
            {
                using var reader = new StreamReader(body);
                var str = await reader.ReadToEndAsync();
                Assert.Equal(StringData, str);
            }
        }

        await using var protocolHolder = Context.CreateRequest<Echo>();
        var array = Encoding.UTF8.GetBytes(StringData);
        await EvaluateAndCompareBody(array);

        Memory<byte> memory = Encoding.UTF8.GetBytes(StringData);
        await EvaluateAndCompareBody(memory);

        ReadOnlyMemory<byte> romemory = Encoding.UTF8.GetBytes(StringData);
        await EvaluateAndCompareBody(romemory);

        var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(StringData));
        await EvaluateAndCompareBody(sequence);

        var str = StringData;
        await EvaluateAndCompareBody(str);

        var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(StringData));
        await EvaluateAndCompareBody(arraySegment);

        await using var body = new Body(protocolHolder, StringData);
        await EvaluateAndCompareBody(body);

        await using var swapping = new SwappingStream(Encoding.UTF8.GetBytes(StringData));
        await EvaluateAndCompareBody(swapping);

        await using var fileStream = File.Open(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
        await fileStream.WriteAsync(Encoding.UTF8.GetBytes(StringData));
        fileStream.Seek(0, SeekOrigin.Begin);

        await EvaluateAndCompareBody(fileStream);
    }

    [Fact]
    public async Task BodyFromSerializedTypes()
    {
        await using var protocolHolder = Context.CreateRequest<Echo>();

        var dict = new Dictionary<string, object>
        {
            ["First"] = 1,
            ["Second"] = "Second"
        };
        await using var body = new Body(protocolHolder, dict);

        await foreach (var item in body.DeserializeAsyncEnumerable<Dictionary<string, object>>())
        {
            Assert.Equal(2, item.Count);
            Assert.Equal(1, item["First"]);
            Assert.Equal("Second", item["Second"]);
        }

        var obj = new
        {
            First = 1,
            Second = "Second"
        };
        await using var body2 = new Body(protocolHolder, obj);

        await foreach (var item in body2.DeserializeAsyncEnumerable<Dictionary<string, object>>())
        {
            Assert.Equal(2, item.Count);
            Assert.Equal(1, item["First"]);
            Assert.Equal("Second", item["Second"]);
        }
    }

    [Fact]
    public async Task DaisyChainingBodies()
    {
        await using var request = Context.CreateRequest<Echo>();
        var dict = new Dictionary<string, object>
        {
            ["First"] = 1,
            ["Second"] = "Second",
            ["Third"] = 3,
            ["Fourth"] = new[] { 1, 2, 3, 4 }
        };
        await using var response = await request
            .WithMethod(Method.POST)
            .WithBody(dict)
            .GetAndSerializeResult();
        await using var response2 = await request
            .WithMethod(Method.POST)
            .WithBody(response.Body)
            .GetAndSerializeResult();
    }


    [Fact]
    public async Task AsyncInit()
    {
        await using var request = Context.CreateRequest<Echo>();
        var dict = new Dictionary<string, object>
        {
            ["First"] = 1,
            ["Second"] = "Second",
            ["Third"] = 3,
            ["Fourth"] = new[] { 1, 2, 3, 4 }
        };
        var asyncEnum = AsyncEnumerable.Repeat(dict, 5);
        var response1 = await request
            .WithMethod(Method.POST)
            .WithBody(asyncEnum)
            .GetResultEntities()
            .CountAsync();
        Assert.Equal(5, response1);
    }
}
