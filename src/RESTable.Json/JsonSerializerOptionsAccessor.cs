using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public class JsonSerializerOptionsAccessor : IJsonSerializerOptionsAccessor
    {
        public JsonSerializerOptions Options { get; }

        public JsonSerializerOptionsAccessor(JsonSerializerOptions options)
        {
            Options = options;
        }
    }
}